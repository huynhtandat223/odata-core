using CFW.ODataCore.Testings.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace CFW.ODataCore.Testings.TestCases;

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
