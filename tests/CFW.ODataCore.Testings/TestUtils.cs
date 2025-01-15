using CFW.ODataCore.Models;
using CFW.ODataCore.Testings;
using FluentAssertions.Equivalency;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;

namespace CFW.ODataCore.Testings;

public static class TestUtils
{
    public static string GetBaseUrl(this Type resourceType, string? routePrefix = null, EntityMethod? excludedMethod = null)
    {
        var odataRouting = resourceType
            .GetCustomAttributes<EntityAttribute>()
            .GroupBy(x => new { x.Name, RoutePrefix = x.RoutePrefix ?? Constants.DefaultODataRoutePrefix, x.Methods })
            .Select(x => x.Key);

        var defaultMethods = Enum.GetValues<EntityMethod>();
        var defaultRoutePrefix = routePrefix ?? Constants.DefaultODataRoutePrefix;

        return excludedMethod is null
            ? odataRouting
                .Where(x => x.RoutePrefix == defaultRoutePrefix && x.Methods.Length == defaultMethods.Length)
                .Select(x => $"{x.RoutePrefix}/{x.Name}")
                .First()
            : odataRouting
                .Where(x => x.RoutePrefix == defaultRoutePrefix && !x.Methods.Contains(excludedMethod.Value))
                .Select(x => $"{x.RoutePrefix}/{x.Name}")
                .First();
    }

    public static string GetNonKeyActionUrl(this Type resourceType, Type handlerType, string? routePrefix = null)
    {
        var actionName = handlerType.GetCustomAttribute<BoundOperationAttribute>()!.OperationName;
        return $"{GetBaseUrl(resourceType, routePrefix)}/{actionName}";
    }

    public static string GetNonKeyFunctionUrl(this Type resourceType, Type handlerType, object queryParams, string? routePrefix = null)
    {
        var actionName = handlerType.GetCustomAttribute<BoundOperationAttribute>()!.OperationName;
        var queryString = queryParams.ParseToQueryString();
        return $"{GetBaseUrl(resourceType, routePrefix)}/{actionName}?{queryString}";
    }

    //public static string GetUnboundActionUrl(this Type handlerType, string? routePrefix = null)
    //{
    //    var unboundActionAttribute = handlerType.GetCustomAttribute<UnboundOperationAttribute>();
    //    if (unboundActionAttribute == null)
    //    {
    //        throw new InvalidOperationException($"The handler type {handlerType.Name} does not have UnboundActionAttribute");
    //    }

    //    return $"{routePrefix ?? unboundActionAttribute.RoutePrefix ?? Constants.DefaultODataRoutePrefix}/{unboundActionAttribute.Name}";
    //}

    //public static string GetUnboundFunctionUrl(this Type handlerType, object requestParams, string? routePrefix = null)
    //{
    //    var attribute = handlerType.GetCustomAttribute<UnboundFunctionAttribute>();
    //    if (attribute == null)
    //    {
    //        throw new InvalidOperationException($"The handler type {handlerType.Name} does not have UnboundFunctionAttribute");
    //    }

    //    if (requestParams == null) throw new ArgumentNullException(nameof(requestParams));

    //    var properties = requestParams.GetType()
    //                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //                        .Where(p => p.GetValue(requestParams) != null);

    //    var queryString = requestParams.ParseToQueryString();

    //    return $"{routePrefix ?? attribute.RoutePrefix ?? Constants.DefaultODataRoutePrefix}/{attribute.Name}?{queryString}";
    //}

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
        var actionName = handlerType.GetCustomAttribute<BoundOperationAttribute>()!.OperationName;
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

    public static ODataQueryResult<object> GetODataQueryResult(this HttpResponseMessage response, Type elementType)
    {
        var content = response.Content.ReadAsStringAsync().Result;
        var odataQueryResultType = typeof(ODataQueryResult<>).MakeGenericType(elementType);
        var responseQueryResult = content.ToType(odataQueryResultType)!;

        var totalCount = nameof(ODataQueryResult<object>.TotalCount);
        var value = nameof(ODataQueryResult<object>.Value);

        return new ODataQueryResult<object>
        {
            TotalCount = responseQueryResult.GetPropertyValue(totalCount) as int?,
            Value = (responseQueryResult.GetPropertyValue(value) as IEnumerable)!.OfType<object>()
        };
    }

    public static object GetResponseResult(this HttpResponseMessage response, Type responseType)
    {
        var content = response.Content.ReadAsStringAsync().Result;
        return content.JsonConvert(responseType);
    }

    /// <summary>
    /// Get the JSON element properties of an array in the response message
    /// </summary>
    /// <param name="httpResponseMessage"></param>
    /// <param name="jsonPropertyName"></param>
    /// <returns></returns>
    public static List<string> GetJsonElementPropertiesInArray(this HttpResponseMessage httpResponseMessage, string arrayProperty)
    {
        var content = httpResponseMessage.Content.ReadAsStringAsync().Result;

        // Validate the JSON body only contains the selected properties
        var responseJson = JsonDocument.Parse(content);
        var responseJsonRoot = responseJson.RootElement;

        if (responseJsonRoot.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("The response body is not a JSON object");

        var santityArraryProperty = arrayProperty.ToLower();
        var arrayJson = responseJsonRoot.EnumerateObject().FirstOrDefault(x => x.Name.ToLower() == santityArraryProperty);

        if (arrayJson.Value.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"The property {arrayProperty} is not an array");

        return arrayJson.Value.EnumerateArray().First().EnumerateObject().Select(x => x.Name).ToList();
    }


    /// <summary>
    /// Parse the response OBJECT properties to a dictionary
    /// </summary>
    /// <param name="httpResponseMessage"></param>
    /// <param name="isExcludeODataContextProp"></param>
    /// <returns></returns>
    public static Dictionary<string, object> ParseToDictionary(this HttpResponseMessage httpResponseMessage
        , bool isExcludeODataContextProp = true)
    {
        var content = httpResponseMessage.Content.ReadAsStringAsync().Result;
        var dic = content.JsonConvert<Dictionary<string, object>>();

        if (isExcludeODataContextProp)
        {
            dic.Remove("@odata.context");
        }
        return dic;
    }

    public static string[] GetComplexTypeProperties(this Type type)
    {
        return type.GetProperties()
            .Where(x => x.PropertyType.IsClass && x.PropertyType.Namespace!.StartsWith("CFW."))
            .Select(x => x.Name)
            .ToArray();
    }

    public static string[] GetCollectionTypeProperties(this Type type)
    {
        var collectionProperties = type.GetProperties()
            .Where(x => x.PropertyType.IsCommonGenericCollectionType())
            .Select(x => x.Name)
            .ToArray();

        return collectionProperties;
    }
}

