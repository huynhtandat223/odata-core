using CFW.ODataCore.Features.BoundActions;
using CFW.ODataCore.Features.UnBoundActions;
using CFW.ODataCore.Testings;
using FluentAssertions.Equivalency;
using System.Reflection;
using System.Text;

namespace CFW.ODataCore.Testings;

public static class TestUtils
{
    public static string GetBaseUrl(this Type resourceType, string? routePrefix = null)
    {
        var odataRouting = resourceType.GetCustomAttribute<ODataEntitySetAttribute>();
        if (odataRouting == null)
        {
            throw new InvalidOperationException($"The resource type {resourceType.Name} does not have ODataEntitySetAttribute");
        }

        return $"{routePrefix ?? odataRouting.RouteRefix ?? Constants.DefaultODataRoutePrefix}/{odataRouting!.Name}";
    }

    public static string GetNonKeyActionUrl(this Type resourceType, Type handlerType, string? routePrefix = null)
    {
        var actionName = handlerType.GetCustomAttribute<BoundActionAttribute>()!.Name;
        return $"{GetBaseUrl(resourceType, routePrefix)}/{actionName}";
    }

    public static string GetUnboundActionUrl(this Type handlerType, string? routePrefix = null)
    {
        var unboundActionAttribute = handlerType.GetCustomAttribute<UnboundActionAttribute>();
        if (unboundActionAttribute == null)
        {
            throw new InvalidOperationException($"The handler type {handlerType.Name} does not have UnboundActionAttribute");
        }

        return $"{routePrefix ?? unboundActionAttribute.RouteRefix ?? Constants.DefaultODataRoutePrefix}/{unboundActionAttribute.Name}";
    }

    public static IEnumerable<string> GetKeyedActionUrl(this Type resourceType, Type handlerType, object keyValue, string? routePrefix = null)
    {
        var actionName = handlerType.GetCustomAttribute<BoundActionAttribute>()!.Name;
        yield return $"{GetBaseUrl(resourceType, routePrefix)}/{keyValue}/{actionName}";
        yield return $"{GetBaseUrl(resourceType, routePrefix)}({keyValue})/{actionName}";
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
