using CFW.ODataCore.Testings.Models;

namespace CFW.ODataCore.Testings.TestCases;

public class EntityQueryDefaultConfigureTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EntityQueryDefaultConfigureTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Category), typeof(Product)])
    {
    }

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task Query_NoAnyParameters_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var complexProps = dbModelType.GetComplexTypeProperties();

        var initialData = await SeedData(dbModelType, 6);

        // Act
        var response = await client.GetAsync(baseUrl);

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        actual.TotalCount.Should().BeNull();

        var expected = initialData.OfType<object>();
        actual.Value.Should()
            .BeEquivalentTo(expected, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryCount_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var complexProps = dbModelType.GetComplexTypeProperties();

        var numberOfItems = 6;
        var initialData = await SeedData(dbModelType, numberOfItems);

        // Act
        var response = await client.GetAsync($"{baseUrl}?$count=true");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        actual.TotalCount.Should().Be(numberOfItems);
        actual.Value.Should().BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QuerySelect_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var numberOfItems = 6;
        var initialData = await SeedData(dbModelType, numberOfItems);
        var complexTypes = dbModelType.GetComplexTypeProperties();

        // Act
        var randomProperties = dbModelType
            .GetProperties()
            .Where(x => !complexTypes.Contains(x.Name))
            .Select(x => x.Name)
            .Random(2);

        var selectQuery = "?$select=" + string.Join(",", randomProperties);
        var response = await client.GetAsync($"{baseUrl}{selectQuery}");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        var filteredActualValues = actual.Value!.Select(randomProperties);
        var filteredExpectedValues = initialData.Select(randomProperties);

        filteredActualValues.Should().BeEquivalentTo(filteredExpectedValues);


        //TODO: need to check how to ignore case in the comparison
        var jsonProperties = response.GetJsonElementPropertiesInArray(nameof(actual.Value));
        jsonProperties.Select(x => x.ToLower()).Should().BeEquivalentTo(randomProperties.Select(x => x.ToLower())
            , o => o.WithoutStrictOrdering());
    }

    [Theory]
    [InlineData(typeof(Category), true)]
    [InlineData(typeof(Product), true)]
    [InlineData(typeof(Category), false)]
    [InlineData(typeof(Product), false)]
    public async Task QueryOrderBy_Success(Type dbModelType, bool isDesc)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var numberOfItems = 6;
        var initialData = await SeedData(dbModelType, numberOfItems);
        var complexProps = dbModelType.GetComplexTypeProperties();

        // Act
        var randomProperty = dbModelType
            .GetProperties()
            .Where(x => complexProps.All(c => c != x.Name))
            .Random().Name;

        var response = await client.GetAsync($"{baseUrl}?$orderby={randomProperty}{(isDesc ? " desc" : string.Empty)}");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        var actualValues = actual.Value
            .OrderByProperty("Id")
            .Select([randomProperty]);

        var expectedValues = initialData.OrderByProperty(randomProperty, !isDesc)
            .OrderByProperty("Id")
            .Select([randomProperty]);

        actualValues.Should().BeEquivalentTo(expectedValues, o => o
            .WithStrictOrdering());
    }

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryFilterEqual_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var numberOfItems = 6;
        var initialData = await SeedData(dbModelType, numberOfItems);
        var complexProps = dbModelType.GetComplexTypeProperties();

        // Act
        var randomProperty = dbModelType
            .GetProperties()
            .Where(x => !x.PropertyType.IsDateTimeType()) //DateTime has use case in the next test
            .Where(x => complexProps.All(c => c != x.Name))
            .Random();
        var randomValue = initialData
            .Random()
            .GetPropertyValue(randomProperty.Name);

        var filterValue = randomValue!.FormatOdataFilter();

        var response = await client.GetAsync($"{baseUrl}?$filter={randomProperty.Name} eq {filterValue}");

        // Assert
        response.Should().BeSuccessful();

        var actual = response.GetODataQueryResult(dbModelType);
        var actualValues = actual.Value!.Select(x => x.GetPropertyValue(randomProperty.Name)).ToList()!;

        actualValues.Should().Contain(randomValue!);
    }

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryTopSkip_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var numberOfItems = 6;
        var initialData = await SeedData(dbModelType, numberOfItems);
        var complexProps = dbModelType.GetComplexTypeProperties();

        // Act
        var skip = 2;
        var top = 2;
        var response = await client.GetAsync($"{baseUrl}?$skip={skip}&$top={top}&$orderby=id");

        // Assert
        response.Should().BeSuccessful();
        var actualValues = response.GetODataQueryResult(dbModelType).Value;
        actualValues.Should().HaveCount(2);

        var orderedInitialData = initialData.OrderByProperty("Id");
        var expectedValues = orderedInitialData.Skip(skip).Take(top);

        actualValues.Should().BeEquivalentTo(expectedValues, o => o
            .WithStrictOrdering().Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory]
    [InlineData(typeof(Product))]
    public async Task QueryExpand_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var numberOfItems = 6;
        var initialData = await SeedData(dbModelType, numberOfItems);
        var complexProps = dbModelType.GetComplexTypeProperties();

        // Act
        var expandQuery = string.Join(",", complexProps);
        var response = await client.GetAsync($"{baseUrl}?$expand={expandQuery}");

        // Assert
        response.Should().BeSuccessful();
        var actualValues = response.GetODataQueryResult(dbModelType).Value;

        //response inclues the expanded property
        initialData.Should().BeEquivalentTo(actualValues);
    }
}
