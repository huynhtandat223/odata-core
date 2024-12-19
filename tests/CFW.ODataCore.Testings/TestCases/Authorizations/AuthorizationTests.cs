using CFW.ODataCore.Testings.TestCases.Authorizations.Models;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Authorizations;

public class AuthorizationTests : BaseTests, IClassFixture<WebApplicationFactory<Program>>
{
    public AuthorizationTests(ITestOutputHelper testOutputHelper, WebApplicationFactory<Program> factory)
        : base(testOutputHelper, factory)
    {
    }

    [Fact]
    public async Task Get_Endpoint_ShouldSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = typeof(AuthorizeCategory).GetBaseUrl();

        // Act
        var responseMessage = await client.GetAsync(baseUrl);
        // Assert
        responseMessage.IsSuccessStatusCode.Should().BeFalse();
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
