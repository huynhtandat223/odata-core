using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;
using System.Web;

namespace CFW.ODataCore.RequestHandlers;

public static class HttpRequestExtensions
{
    public static async Task<T> ParseRequest<T>(this IHttpRequestHandler _, HttpRequest request)
    {
        var jsonOption = request.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

        if (request.Method == HttpMethod.Get.Method)
        {
            if (!request.QueryString.HasValue)
            {
                return default(T)!;
            }

            var dict = HttpUtility.ParseQueryString(request.QueryString.Value);
            string json = JsonSerializer.Serialize(dict.Cast<string>().ToDictionary(k => k, v => dict[v]));
            var model = JsonSerializer.Deserialize<T>(json, jsonOption.JsonSerializerOptions);
            return model!;
        }

        if (!request.Body.CanRead)
        {
            throw new InvalidOperationException("Request body is not readable");
        }

        using var reader = new StreamReader(request.Body);
        var bodyAsString = await reader.ReadToEndAsync();
        using var document = JsonDocument.Parse(bodyAsString);
        var root = document.RootElement;

        var jsonObject = root.EnumerateObject();
        var isBodyWrapper = jsonObject.Count() == 1
            && jsonObject.Any(x => x.Name.Equals("body", StringComparison.CurrentCultureIgnoreCase));
        if (isBodyWrapper)
        {
            var rootProperty = jsonObject.First();
            return BindJsonElement<T>(request, rootProperty.Value, jsonOption);
        }

        return BindJsonElement<T>(request, root, jsonOption);
    }

    private static T BindJsonElement<T>(HttpRequest request, JsonElement jsonElment, JsonOptions jsonOptions)
    {
        var result = jsonElment.Deserialize<T>(jsonOptions.JsonSerializerOptions);
        if (result is null)
        {
            throw new InvalidOperationException("Failed to deserialize request body");
        }

        var routeData = request.RouteValues;
        var routeBoundProperties = typeof(T).GetProperties()
            .Where(property => property.GetCustomAttribute<FromRouteAttribute>() is not null);

        foreach (var routeBoundProperty in routeBoundProperties)
        {
            var routeAttribute = routeBoundProperty.GetCustomAttribute<FromRouteAttribute>();
            var routeKey = routeAttribute?.Name ?? routeBoundProperty.Name;

            if (routeData.TryGetValue(routeKey, out var routeValue))
            {
                var convertedRouteValue = Convert.ChangeType(routeValue, routeBoundProperty.PropertyType);
                routeBoundProperty.SetValue(result, convertedRouteValue);
            }
        }

        return result;
    }
}


