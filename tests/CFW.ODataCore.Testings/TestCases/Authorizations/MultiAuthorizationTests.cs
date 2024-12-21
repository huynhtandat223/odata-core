using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.Extensions;
using CFW.ODataCore.OData;
using CFW.ODataCore.Testings.TestCases.Authorizations.Models;
using Microsoft.AspNetCore.TestHost;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Authorizations;

public class MultiAuthorizationTests : BaseTests, IClassFixture<NonInitAppFactory>
{
    private readonly Type multiAuthorizationType = typeof(MultiAuthorization);
    private const string testApi = "test-api";

    public MultiAuthorizationTests(ITestOutputHelper testOutputHelper, NonInitAppFactory nonInitAppFactory)
        : base(testOutputHelper, nonInitAppFactory)
    {
        _factory = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddControllers().AddGenericODataEndpoints(new TestODataTypeResolver(testApi, [multiAuthorizationType]));
                });
            });
    }

    [Fact]
    public async Task Request_MultiAuthorization_ShouldSuccess()
    {
        var admin = Guid.NewGuid().ToString();
        var password = DefaultPassword;

        var superAdmin = Guid.NewGuid().ToString();

        var seedUserInfos = new List<SeedUserInfo>
        {
            new SeedUserInfo
            {
                UserName = admin,
                Password = password,
                Roles = [TestUtils.AdminRole]
            },
            new SeedUserInfo
            {
                UserName = superAdmin,
                Password = password,
                Roles = [TestUtils.SupperAdminRole, TestUtils.AdminRole]
            }
        };

        await SeedUsers(seedUserInfos);
        var client = _factory.CreateClient();

        var adminToken = await client.LoginAndGetToken(admin, password);
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

        var superAdminToken = await client.LoginAndGetToken(superAdmin, password);
        var superAdminClient = _factory.CreateClient();
        superAdminClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {superAdminToken}");

        var baseUrl = multiAuthorizationType.GetBaseUrl(testApi);
        var defaultModel = DataGenerator.Create(multiAuthorizationType);
        var dbContext = _factory.Services.GetRequiredService<TestingDbContext>();
        dbContext.Add(defaultModel);
        dbContext.SaveChanges();
        var id = defaultModel.GetPropertyValue(nameof(IODataViewModel<object>.Id));

        //Query - Only need authorization.
        var unauthorizedResponse = await client.GetAsync(baseUrl);
        unauthorizedResponse.IsSuccessStatusCode.Should().BeFalse();
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var adminQueryResponse = await adminClient.GetAsync(baseUrl);
        adminQueryResponse.IsSuccessStatusCode.Should().BeTrue();

        var superAdminQueryResponse = await superAdminClient.GetAsync(baseUrl);
        superAdminQueryResponse.IsSuccessStatusCode.Should().BeTrue();

        //GetByKey - Need Admin Role
        var unauthorizedGetByKeyResponse = await client.GetAsync($"{baseUrl}/{id}");
        unauthorizedGetByKeyResponse.IsSuccessStatusCode.Should().BeFalse();
        unauthorizedGetByKeyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var adminGetByKeyResponse = await adminClient.GetAsync($"{baseUrl}/{id}");
        adminGetByKeyResponse.IsSuccessStatusCode.Should().BeTrue();

        var superAdminGetByKeyResponse = await superAdminClient.GetAsync($"{baseUrl}/{id}");
        superAdminGetByKeyResponse.IsSuccessStatusCode.Should().BeTrue();

        //PostCreate - Need Admin or SuperAdmin Role
        var unauthorizedPostCreateResponse = await client.PostAsJsonAsync(baseUrl, DataGenerator.Create(multiAuthorizationType));
        unauthorizedPostCreateResponse.IsSuccessStatusCode.Should().BeFalse();
        unauthorizedPostCreateResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var adminPostCreateResponse = await adminClient.PostAsJsonAsync(baseUrl, DataGenerator.Create(multiAuthorizationType));
        adminPostCreateResponse.IsSuccessStatusCode.Should().BeTrue();

        var superAdminPostCreateResponse = await superAdminClient.PostAsJsonAsync(baseUrl, DataGenerator.Create(multiAuthorizationType));
        superAdminPostCreateResponse.IsSuccessStatusCode.Should().BeTrue();

        //Delete - Need SuperAdmin Role
        var unauthorizedDeleteResponse = await client.DeleteAsync($"{baseUrl}/{id}");
        unauthorizedDeleteResponse.IsSuccessStatusCode.Should().BeFalse();
        unauthorizedDeleteResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var adminDeleteResponse = await adminClient.DeleteAsync($"{baseUrl}/{id}");
        adminDeleteResponse.IsSuccessStatusCode.Should().BeFalse();
        adminDeleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var superAdminDeleteResponse = await superAdminClient.DeleteAsync($"{baseUrl}/{id}");
        superAdminDeleteResponse.IsSuccessStatusCode.Should().BeTrue();
    }
}
