using CFW.ODataCore.Testings.Models;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;

namespace CFW.ODataCore.Testings.TestCases;

public class EntityQueryDisableQueryOptionsAsGlobalTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EntityQueryDisableQueryOptionsAsGlobalTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Category), typeof(Product)])
    {
    }

    private (HttpClient httpClient, List<object> initialData) SetupAllowQueryOptions(Action<DefaultQueryConfigurations> setup
        , Type dbModelType, int? seedDataNumber = 6)
    {
        List<object>? initialData = null;
        var httpClient = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.PostConfigure<ODataOptions>(o =>
                    {
                        setup(o.QueryConfigurations);
                    });

                    if (seedDataNumber is not null)
                        initialData = SeedData(dbModelType, seedDataNumber.Value, services);
                });
            })
            .CreateClient();

        return (httpClient, initialData!);
    }


    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryDisableCount_SuccessWithNoTotalCountResponse(Type dbModelType)
    {
        // Arrange
        var defaultQueryConfig = _factory.Services.GetService<IOptions<ODataOptions>>()!.Value.QueryConfigurations;

        var (client, _) = SetupAllowQueryOptions(o => o.EnableCount = false, dbModelType);
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();

        // Act
        var response = await client.GetAsync($"{baseUrl}?$count=true");

        // Assert
        response.Should().BeSuccessful();

        var actual = response.GetODataQueryResult(dbModelType);
        actual.TotalCount.Should().BeNull();
    }

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryDisableFilter_SuccessWithAllData(Type dbModelType)
    {
        // Arrange
        var dataCount = 6;
        var (client, initialData) = SetupAllowQueryOptions(o => o.EnableFilter = false, dbModelType, dataCount);
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var complexProps = dbModelType.GetComplexTypeProperties();

        // Act
        var response = await client.GetAsync($"{baseUrl}?$filter eq '{Guid.NewGuid().ToString()}'&$count=true");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        //Filter is disabled, so all data should be returned
        actual.TotalCount.Should().Be(dataCount);
        actual.Value.Should().BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryDisableOrderBy_SuccessWithAllData(Type dbModelType)
    {
        // Arrange

        var dataCount = 6;
        var (client, initialData) = SetupAllowQueryOptions(o => o.EnableOrderBy = false, dbModelType, dataCount);
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var complexProps = dbModelType.GetComplexTypeProperties();

        // Act
        var response = await client.GetAsync($"{baseUrl}?$orderby={Guid.NewGuid().ToString()}&$count=true");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        //OrderBy is disabled, so all data should be returned
        actual.TotalCount.Should().Be(dataCount);
        actual.Value.Should()
            .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory(Skip = "Select can't disable ????")]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryDisableSelect_SuccessWithAllData(Type dbModelType)
    {
        // Arrange
        var dataCount = 6;
        var (client, initialData) = SetupAllowQueryOptions(o => o.EnableSelect = false, dbModelType, dataCount);
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var complexProps = dbModelType.GetComplexTypeProperties();

        var randomProperties = dbModelType
            .GetProperties()
            .Where(x => !complexProps.Contains(x.Name))
            .Select(x => x.Name)
            .Random(2);
        var selectQuery = "?$select=" + string.Join(",", randomProperties);

        // Act
        var response = await client.GetAsync($"{baseUrl}{selectQuery}");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        //Select is disabled, so all data should be returned
        actual.Value.Should()
            .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory(Skip = "Global config can't set skip")]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryDisableSkip_SuccessWithAllData(Type dbModelType)
    {
        // Arrange
        var dataCount = 6;
        var (client, initialData) = SetupAllowQueryOptions(o => o.EnableSkipToken = false, dbModelType, dataCount); //Don't have EnableSkip property
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var complexProps = dbModelType.GetComplexTypeProperties();

        // Act
        var response = await client.GetAsync($"{baseUrl}?$skip=1&$count=true");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        //Skip is disabled, so all data should be returned
        actual.TotalCount.Should().Be(dataCount);
        actual.Value.Should()
            .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory(Skip = "Global config can't set top")]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryDisableTop_SuccessWithAllData(Type dbModelType)
    {
        // Arrange
        var dataCount = 6;
        var (client, initialData) = SetupAllowQueryOptions(o => o.MaxTop = null, dbModelType, dataCount); //Don't have EnableTop property
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var complexProps = dbModelType.GetComplexTypeProperties();

        // Act
        var response = await client.GetAsync($"{baseUrl}?$top=1&$count=true");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        //Top is disabled, so all data should be returned
        actual.TotalCount.Should().Be(dataCount);
        actual.Value.Should()
            .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }


    [Theory]
    [InlineData(typeof(Product))]
    public async Task QueryDisableExpand_SuccessWithAllData(Type dbModelType)
    {
        // Arrange
        var complexTypes = dbModelType.GetComplexTypeProperties();
        var dataCount = 6;
        var expandProps = complexTypes;
        var (client, initialData) = SetupAllowQueryOptions(o => o.EnableExpand = false, dbModelType, dataCount);
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();

        // Act
        var expandQuery = "?$expand=" + string.Join(",", expandProps);
        var propertyNames = dbModelType.GetProperties().Select(x => x.Name);
        var selectQuery = "&$select=" + string.Join(",", propertyNames);
        var response = await client.GetAsync($"{baseUrl}{expandQuery}{selectQuery}");

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        //actual expand properties should be null
        var expandPropsList = actual.Value.Select(expandProps);
        foreach (var expandProp in expandPropsList)
        {
            expandProp.Should().AllSatisfy(expandValue => expandValue.Value.Should().BeNull());
        }

        //Disable Expand, so only the primitive properties should be returned
        actual.Value.Should().BeEquivalentTo(initialData, o => o.Excluding(e => expandProps.Contains(e.Name)));
    }
}
