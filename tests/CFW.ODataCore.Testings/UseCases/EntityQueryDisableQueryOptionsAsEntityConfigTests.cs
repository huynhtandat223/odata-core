using CFW.ODataCore.Testings.Models;
using CFW.ODataCore.Testings.TestCases;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.TestHost;

namespace CFW.ODataCore.Testings.UseCases;

public class EntityQueryDisableQueryOptionsAsEntityConfigTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EntityQueryDisableQueryOptionsAsEntityConfigTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Category), typeof(Product)])
    {
    }

    private (HttpClient httpClient, List<object> initialData) SetupAllowQueryOptions(AllowedQueryOptions allowedQueryOptions
        , Type dbModelType, int? seedDataNumber = 6)
    {
        List<object>? initialData = null;
        var httpClient = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var entityConfigurationType = typeof(EntityConfiguration<>).MakeGenericType(dbModelType);
                    var entityConfigurationServiceDesc = services.First(s => s.ServiceType == entityConfigurationType);

                    if (entityConfigurationServiceDesc.ImplementationInstance is not EntityEndpointConfiguration entityEndpoint)
                        throw new Exception("EntityEndpoint should be EntityConfiguration");

                    if (entityEndpoint.QueryOptionConfig != null)
                        throw new Exception("QueryOptionConfig should be null");

                    entityEndpoint.QueryOptionConfig = x =>
                    {
                        x.AllowedQueryOptions = allowedQueryOptions;
                    };

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
        var (client, _) = SetupAllowQueryOptions(~AllowedQueryOptions.Count, dbModelType);
        var baseUrl = dbModelType.GetBaseUrl();

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
        var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Filter, dbModelType, dataCount);
        var baseUrl = dbModelType.GetBaseUrl();
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
        var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.OrderBy, dbModelType, dataCount);
        var baseUrl = dbModelType.GetBaseUrl();
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
        var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Select, dbModelType, dataCount);
        var baseUrl = dbModelType.GetBaseUrl();
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

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryDisableSkip_SuccessWithAllData(Type dbModelType)
    {
        // Arrange
        var dataCount = 6;
        var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Skip, dbModelType, dataCount);
        var baseUrl = dbModelType.GetBaseUrl();
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

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryDisableTop_SuccessWithAllData(Type dbModelType)
    {
        // Arrange
        var dataCount = 6;
        var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Top, dbModelType, dataCount);
        var baseUrl = dbModelType.GetBaseUrl();
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
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public async Task QueryDisableTopAndSkip_SuccessWithAllData(Type dbModelType)
    {
        // Arrange
        var dataCount = 6;
        var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Top & ~AllowedQueryOptions.Skip, dbModelType, dataCount);
        var baseUrl = dbModelType.GetBaseUrl();
        var complexProps = dbModelType.GetComplexTypeProperties();

        // Act

        var response = await client.GetAsync($"{baseUrl}?$top=1&$skip=1&$count=true");
        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        //Top and Skip are disabled, so all data should be returned
        actual.TotalCount.Should().Be(dataCount);
        actual.Value.Should()
            .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory]
    [InlineData(typeof(Product))]
    public async Task QueryDisableExpand_SuccessWithAllData(Type dbModelType)
    {
        // Arrange
        var dataCount = 6;
        var expandProp = dbModelType.GetComplexTypeProperties().Single();
        var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Expand, dbModelType, dataCount);
        var baseUrl = dbModelType.GetBaseUrl();

        // Act
        var response = await client.GetAsync($"{baseUrl}?$expand={expandProp}");

        // Assert
        response.Should().BeSuccessful();
        var content = await response.Content.ReadAsStringAsync();
        var actual = response.GetODataQueryResult(dbModelType);

        actual.Value.Should().BeEquivalentTo(initialData);
    }
}
