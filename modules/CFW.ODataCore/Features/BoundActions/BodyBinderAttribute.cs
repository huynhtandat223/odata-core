using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CFW.ODataCore.Features.BoundActions;


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

        var modelType = bindingContext.ModelType;
        var request = bindingContext.HttpContext.Request;
        var jsonOption = bindingContext.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>();

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
            var count = jsonObject.Count();
            if (count == 1)
            {
                var rootProperty = jsonObject.First();
                var propName = rootProperty.Name;

                if (propName.Equals("body", StringComparison.CurrentCultureIgnoreCase))
                {
                    var bodyProperty = JsonSerializer.Deserialize(rootProperty.Value, modelType, jsonOption.Value.JsonSerializerOptions);
                    if (bodyProperty is null)
                    {
                        bindingContext.Result = ModelBindingResult.Failed();
                        bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid body");
                        return;
                    }

                    bindingContext.Result = ModelBindingResult.Success(bodyProperty);
                    return;
                }
            }

            var result = JsonSerializer.Deserialize(root, modelType, jsonOption.Value.JsonSerializerOptions);
            if (result is null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid body");

                return;
            }

            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, ex.Message);
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }
}
