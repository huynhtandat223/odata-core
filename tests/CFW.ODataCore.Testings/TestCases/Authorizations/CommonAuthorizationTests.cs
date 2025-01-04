using CFW.Core.Entities;
using CFW.ODataCore.Testings.TestCases.Authorizations.Models;
using FluentAssertions.Common;
using System.Net;
using System.Reflection;

namespace CFW.ODataCore.Testings.TestCases.Authorizations;

public class CommonAuthorizationTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public CommonAuthorizationTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [InlineData(typeof(AuthorizeCategory))]
    public async Task Request_DefaultAuthorize_AllMethodsUnauthorized(Type resourceType)
    {
        // Arrange
        var authorizeAttributes = resourceType
            .GetCustomAttributes<EntityAuthorizeAttribute>();
        if (authorizeAttributes.Any() == false)
        {
            throw new InvalidOperationException("Test data invalid.No authorize attribute found.");
        }

        var client = _factory.CreateClient();
        var baseUrl = resourceType.GetBaseUrl();
        var data = DataGenerator.Create(resourceType);
        var id = data.GetPropertyValue(nameof(IEntity<object>.Id));


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
            .GetCustomAttributes<EntityAuthorizeAttribute>();
        if (authorizeAttributes.Any() == false)
        {
            throw new InvalidOperationException("Test data invalid.No authorize attribute found.");
        }

        var userName = Guid.NewGuid().ToString();
        var password = DefaultPassword;
        await SeedUser(userName, password);

        var client = _factory.CreateClient();
        var token = await client.LoginAndGetToken(userName, password);

        var baseUrl = resourceType.GetBaseUrl();
        var defaultModel = DataGenerator.Create(resourceType);
        var dbContext = GetDbContext();
        dbContext.Add(defaultModel);
        dbContext.SaveChanges();

        var id = defaultModel.GetPropertyValue(nameof(IEntity<object>.Id));

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        var responseMessage = await client.GetAsync(baseUrl);
        responseMessage.IsSuccessStatusCode.Should().BeTrue();


        responseMessage = await client.PostAsJsonAsync(baseUrl, DataGenerator.Create(resourceType));
        responseMessage.IsSuccessStatusCode.Should().BeTrue();


        responseMessage = await client.GetAsync(baseUrl);
        responseMessage.IsSuccessStatusCode.Should().BeTrue();


        responseMessage = await client.GetAsync($"{baseUrl}/{id}");
        responseMessage.IsSuccessStatusCode.Should().BeTrue();

        responseMessage = await client.PatchAsJsonAsync($"{baseUrl}/{id}", DataGenerator.Create(resourceType));
        responseMessage.IsSuccessStatusCode.Should().BeTrue();


        responseMessage = await client.DeleteAsync($"{baseUrl}/{id}");
        responseMessage.IsSuccessStatusCode.Should().BeTrue();
    }

}
