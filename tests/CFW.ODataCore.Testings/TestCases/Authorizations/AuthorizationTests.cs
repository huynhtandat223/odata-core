using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.OData;
using CFW.ODataCore.Testings.TestCases.Authorizations.Models;
using FluentAssertions.Common;
using System.Net;
using System.Reflection;

namespace CFW.ODataCore.Testings.TestCases.Authorizations;

public class AuthorizationTests : BaseTests, IClassFixture<WebApplicationFactory<Program>>
{
    public AuthorizationTests(ITestOutputHelper testOutputHelper, WebApplicationFactory<Program> factory)
        : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [InlineData(typeof(AuthorizeCategory))]
    public async Task Request_DefaultAuthorize_AllMethodsUnauthorized(Type resourceType)
    {
        // Arrange
        var authorizeAttributes = resourceType
            .GetCustomAttributes<ODataAuthorizeAttribute>();
        if (authorizeAttributes.Any() == false)
        {
            throw new InvalidOperationException("Test data invalid.No authorize attribute found.");
        }

        if (authorizeAttributes.Any(x => x.ApplyMethods != null))
        {
            throw new InvalidOperationException("Test data invalid.Only default authorize attribute is allowed.");
        }

        var client = _factory.CreateClient();
        var baseUrl = resourceType.GetBaseUrl();
        var data = DataGenerator.Create(resourceType);
        var id = data.GetPropertyValue(nameof(IODataViewModel<object>.Id));


        var responseMessage = await client.GetAsync(baseUrl);
        responseMessage.IsSuccessStatusCode.Should().BeFalse();
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);


        responseMessage = await client.PostAsJsonAsync(baseUrl, data);
        responseMessage.IsSuccessStatusCode.Should().BeFalse();
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);


        responseMessage = await client.GetAsync(baseUrl);
        responseMessage.IsSuccessStatusCode.Should().BeFalse();
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);


        responseMessage = await client.DeleteAsync($"{baseUrl}/{id}");
        responseMessage.IsSuccessStatusCode.Should().BeFalse();
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);


        responseMessage = await client.GetAsync($"{baseUrl}/{id}");
        responseMessage.IsSuccessStatusCode.Should().BeFalse();
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);


        responseMessage = await client.PatchAsJsonAsync($"{baseUrl}/{id}", data);
        responseMessage.IsSuccessStatusCode.Should().BeFalse();
        responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(typeof(AuthorizeCategory))]
    public async Task Request_DefaultAuthorize_ValidToken_AllMethodsSuccess(Type resourceType)
    {
        // Arrange
        var authorizeAttributes = resourceType
            .GetCustomAttributes<ODataAuthorizeAttribute>();
        if (authorizeAttributes.Any() == false)
        {
            throw new InvalidOperationException("Test data invalid.No authorize attribute found.");
        }

        if (authorizeAttributes.Any(x => x.ApplyMethods != null))
        {
            throw new InvalidOperationException("Test data invalid.Only default authorize attribute is allowed.");
        }

        var userName = "admin";
        var password = "123!@#abcABC";
        await SeedUser(userName, password);

        var client = _factory.CreateClient();
        var token = await client.LoginAndGetToken(userName, password);

        var baseUrl = resourceType.GetBaseUrl();
        var defaultModel = DataGenerator.Create(resourceType);
        var dbContext = _factory.Services.GetRequiredService<TestingDbContext>();
        dbContext.Add(defaultModel);
        dbContext.SaveChanges();
        var id = defaultModel.GetPropertyValue(nameof(IODataViewModel<object>.Id));

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        var responseMessage = await client.GetAsync(baseUrl);
        responseMessage.IsSuccessStatusCode.Should().BeTrue();


        responseMessage = await client.PostAsJsonAsync(baseUrl, DataGenerator.Create(resourceType));
        responseMessage.IsSuccessStatusCode.Should().BeTrue();


        responseMessage = await client.GetAsync(baseUrl);
        responseMessage.IsSuccessStatusCode.Should().BeTrue();


        responseMessage = await client.GetAsync($"{baseUrl}/{id}");
        responseMessage.IsSuccessStatusCode.Should().BeTrue();


        responseMessage = await client.PatchAsJsonAsync($"{baseUrl}/{id}", defaultModel);
        responseMessage.IsSuccessStatusCode.Should().BeTrue();


        responseMessage = await client.DeleteAsync($"{baseUrl}/{id}");
        responseMessage.IsSuccessStatusCode.Should().BeTrue();

    }
}
