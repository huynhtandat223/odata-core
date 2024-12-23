using CFW.ODataCore.Testings;
using FluentAssertions.Equivalency;
using System.Reflection;
using System.Text;
using System.Web;

namespace CFW.ODataCore.Testings;

public static class TestUtils
{
    public static string GetBaseUrl(this Type resourceType, string? routePrefix = null)
    {
        var odataRouting = resourceType
            .GetCustomAttributes<ODataAPIRoutingAttribute>()
            .GroupBy(x => new { x.Name, x.RouteRefix })
            .Select(x => x.Key)
            .SingleOrDefault();
        if (odataRouting is null)
        {
            throw new InvalidOperationException($"The resource type {resourceType.Name} does not have ODataAPIRoutingAttribute");
        }

        return $"{routePrefix ?? odataRouting.RouteRefix ?? Constants.DefaultODataRoutePrefix}/{odataRouting!.Name}";
    }

    public static string GetNonKeyActionUrl(this Type resourceType, Type handlerType, string? routePrefix = null)
    {
        var actionName = handlerType.GetCustomAttribute<BoundOperationAttribute>()!.Name;
        return $"{GetBaseUrl(resourceType, routePrefix)}/{actionName}";
    }

    public static string GetNonKeyFunctionUrl(this Type resourceType, Type handlerType, object queryParams, string? routePrefix = null)
    {
        var actionName = handlerType.GetCustomAttribute<BoundOperationAttribute>()!.Name;
        var queryString = queryParams.ParseToQueryString();
        return $"{GetBaseUrl(resourceType, routePrefix)}/{actionName}?{queryString}";
    }

    public static string GetUnboundActionUrl(this Type handlerType, string? routePrefix = null)
    {
        var unboundActionAttribute = handlerType.GetCustomAttribute<UnboundOperationAttribute>();
        if (unboundActionAttribute == null)
        {
            throw new InvalidOperationException($"The handler type {handlerType.Name} does not have UnboundActionAttribute");
        }

        return $"{routePrefix ?? unboundActionAttribute.RoutePrefix ?? Constants.DefaultODataRoutePrefix}/{unboundActionAttribute.Name}";
    }

    public static string GetUnboundFunctionUrl(this Type handlerType, object requestParams, string? routePrefix = null)
    {
        var attribute = handlerType.GetCustomAttribute<UnboundFunctionAttribute>();
        if (attribute == null)
        {
            throw new InvalidOperationException($"The handler type {handlerType.Name} does not have UnboundFunctionAttribute");
        }

        if (requestParams == null) throw new ArgumentNullException(nameof(requestParams));

        var properties = requestParams.GetType()
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.GetValue(requestParams) != null);

        var queryString = requestParams.ParseToQueryString();

        return $"{routePrefix ?? attribute.RoutePrefix ?? Constants.DefaultODataRoutePrefix}/{attribute.Name}?{queryString}";
    }

    public static string ParseToQueryString(this object requestParams)
    {
        if (requestParams == null)
            throw new ArgumentNullException(nameof(requestParams));

        var properties = requestParams.GetType()
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.GetValue(requestParams) != null);
        return string.Join("&", properties.Select(property =>
        {
            var name = HttpUtility.UrlEncode(property.Name);
            var value = HttpUtility.UrlEncode(property.GetValue(requestParams)?.ToString());
            return $"{name}={value}";
        }));
    }

    public static IEnumerable<string> GetKeyedActionUrl(this Type resourceType, Type handlerType, object keyValue, string? routePrefix = null)
    {
        var actionName = handlerType.GetCustomAttribute<BoundOperationAttribute>()!.Name;
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
