using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;

namespace CFW.ODataCore.Features.Shared;


[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public class BodyBinderAttribute : ModelBinderAttribute
{
    public BodyBinderAttribute()
    {
        BinderType = typeof(BodyBinder);
    }
}
public class BodyBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        var request = bindingContext.HttpContext.Request;

        if (!request.Body.CanRead)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        try
        {
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
                BindJsonElement(bindingContext, rootProperty.Value);
                return;
            }

            BindJsonElement(bindingContext, root);
            return;
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, ex.Message);
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }

    private void BindJsonElement(ModelBindingContext bindingContext, JsonElement jsonElment)
    {
        var modelType = bindingContext.ModelType;
        var jsonOption = bindingContext.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>();

        var result = jsonElment.Deserialize(modelType, jsonOption.Value.JsonSerializerOptions);
        if (result is null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid body");
            return;
        }

        var routeData = bindingContext.HttpContext.Request.RouteValues;
        var routeBoundProperties = modelType.GetProperties()
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

        bindingContext.Result = ModelBindingResult.Success(result);
        return;
    }
}
