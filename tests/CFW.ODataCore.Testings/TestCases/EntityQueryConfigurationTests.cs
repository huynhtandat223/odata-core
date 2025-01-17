using CFW.ODataCore.Models;
using CFW.ODataCore.Testings.Models;

namespace CFW.ODataCore.Testings.TestCases;

public class EntityQueryConfigurationTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EntityQueryConfigurationTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Category)])
    {
    }

    [Theory]
    [InlineData(typeof(Category))]
    public async Task DisableQueryMethod_NotFoundOrNotAllow(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl(excludedMethod: ApiMethod.Query);

        // Act
        var response = await client.GetAsync($"{baseUrl}");

        // Assert
        response.Should().HaveClientError("Expect 404 or 405");
    }
}
