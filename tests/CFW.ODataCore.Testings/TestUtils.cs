using CFW.ODataCore.OData;
using CFW.ODataCore.Testings;
using FluentAssertions.Equivalency;
using System.Reflection;
using System.Text;

namespace CFW.ODataCore.Testings;

public static class TestUtils
{
    public static string GetDefaultBaseUrl(this Type resourceType, string? routePrefix = null)
    {
        var odataRouting = resourceType.GetCustomAttribute<ODataRoutingAttribute>();
        return $"{routePrefix ?? Constants.DefaultODataRoutePrefix}/{odataRouting!.Name}";
    }

    public const string AdminRole = "Admin";
    public const string SupperAdminRole = "SuperAdmin";

    public static EquivalencyAssertionOptions<TExpectation> CompareDecimal<TExpectation>(
         EquivalencyAssertionOptions<TExpectation> o)
    {
        return o.Using<decimal>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.01M))
            .WhenTypeIs<decimal>();
    }

    public static StringContent ToStringContent(this object content)
    {
        return new StringContent(content.ToJsonString(), Encoding.UTF8, "application/json");
    }

    public static Dictionary<string, object?> ToDictionary(this object obj)
    {
        return obj.GetType().GetProperties()
            .ToDictionary(x => x.Name, x => x.GetValue(obj));
    }

    public static async Task<string> LoginAndGetToken(this HttpClient httpClient, string email, string password)
    {
        var loginResponse = await httpClient.PostAsJsonAsync("/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginResponseContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResponseContent!.AccessToken;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
