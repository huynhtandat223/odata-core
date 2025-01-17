using CFW.ODataCore.Testings.Models;

namespace CFW.ODataCore.Testings.TestCases;

public class EntityGetByKeyDefaultConfigureTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EntityGetByKeyDefaultConfigureTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Category), typeof(Product)])
    {
    }

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task GetByKey_NoAnyParameters_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var complexProps = dbModelType.GetComplexTypeProperties();

        var initialData = await SeedData(dbModelType, 2);
        var item = initialData.Random();
        var key = item.GetPropertyValue(DefaultIdProp);

        // Act
        var response = await client.GetAsync($"{baseUrl}/{key}");

        // Assert
        response.Should().BeSuccessful();
        var data = response.GetResponseResult(dbModelType);

        data.Should().NotBeNull();
        data!.Should().BeEquivalentTo(item, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }


    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task GetByKeySelect_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var numberOfItems = 2;
        var initialData = await SeedData(dbModelType, numberOfItems);
        var complexTypes = dbModelType.GetComplexTypeProperties();

        // Act
        var randomProperties = dbModelType
            .GetProperties()
            .Where(x => !complexTypes.Contains(x.Name))
            .Select(x => x.Name)
            .Random(2);
        var item = initialData.Random();
        var key = item.GetPropertyValue(DefaultIdProp);

        var selectQuery = "?$select=" + string.Join(",", randomProperties);
        var response = await client.GetAsync($"{baseUrl}/{key}{selectQuery}");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetResponseResult(dbModelType);

        actual.Should().BeEquivalentTo(item
            , o => o.Excluding(actual => !randomProperties.Contains(actual.Name)));

        var jsonProperties = response
            .ParseToDictionary();

        jsonProperties.Should().HaveCount(randomProperties.Count);
    }

    [Theory]
    [InlineData(typeof(Product))]
    public async Task QueryExpand_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var numberOfItems = 2;
        var initialData = await SeedData(dbModelType, numberOfItems);
        var complexProps = dbModelType.GetComplexTypeProperties();
        var item = initialData.Random();
        var key = item.GetPropertyValue(DefaultIdProp);

        // Act
        var response = await client.GetAsync($"{baseUrl}/{key}?$expand={complexProps.Single()}");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetResponseResult(dbModelType);

        //response inclues the expanded property
        item.Should().BeEquivalentTo(actual);
    }
}
