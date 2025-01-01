using CFW.ODataCore.ODataMetadata;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System.Reflection;

namespace CFW.ODataCore.Models;

public class ODataModel<TODataViewModel, TKey, TBindingModel>
    where TODataViewModel : class
{
    public TBindingModel? Value { get; set; }

    public static async ValueTask<ODataModel<TODataViewModel, TKey, TBindingModel>?> BindAsync(HttpContext context,
                                                   ParameterInfo parameter)
    {
        var odataFeature = context.Features.Get<IODataFeature>();
        if (odataFeature is null)
            AddODataFeature(context);

        var modelState = new ModelStateDictionary();
        var modelName = parameter.Name!;
        var provider = new EmptyModelMetadataProvider();
        var modelMetadata = provider.GetMetadataForType(typeof(TBindingModel));
        var inputContext = new InputFormatterContext(
            context,
            modelName,
            modelState,
            modelMetadata,
            (stream, encoding) => new StreamReader(stream, encoding)
            );

        InputFormatterResult? inputResult = default;
        foreach (var inputFormatter in ODataInputFormatterFactory.Create().Reverse())
        {
            var canRead = inputFormatter.CanRead(inputContext);

            if (canRead)
            {
                inputResult = await inputFormatter.ReadAsync(inputContext);
                break;
            }
        }

        if (inputResult is null)
            return null;

        return new ODataModel<TODataViewModel, TKey, TBindingModel>
        {
            Value = (TBindingModel)inputResult.Model!
        };
    }

    public static void AddODataFeature(HttpContext httpContext)
    {
        var container = httpContext.GetEndpoint()?.Metadata.OfType<ODataMetadataContainer>().SingleOrDefault();
        if (container is null)
            throw new InvalidOperationException("ODataMetadataContainer not found");

        var odataOptions = httpContext.RequestServices.GetRequiredService<IOptions<ODataOptions>>().Value;

        var entityName = httpContext.GetEndpoint()?.Metadata.OfType<EntityCRUDRoutingMetadata>().SingleOrDefault()?.Name;

        if (entityName.IsNullOrWhiteSpace())
            entityName = httpContext.GetEndpoint()?.Metadata.OfType<EntityOperationMetadata>().SingleOrDefault()?.EntityRoutingName;

        if (entityName is null)
            throw new InvalidOperationException("Entity name not found");

        var edmEntitySet = container.EdmModel.EntityContainer.FindEntitySet(entityName);
        var entitySetSegment = new EntitySetSegment(edmEntitySet);
        var segments = new List<ODataPathSegment> { entitySetSegment };

        if (httpContext.Request.RouteValues.TryGetValue("key", out var key))
        {
            var entityType = edmEntitySet.EntityType();
            var keyName = entityType.DeclaredKey.Single();
            var keySegment = new KeySegment(new Dictionary<string, object> { { keyName.Name, key! } }, entityType
                , edmEntitySet);
            segments.Add(keySegment);
        }

        var path = new ODataPath(segments);
        var feature = new ODataFeature
        {
            Path = path,
            Model = container.EdmModel,
            RoutePrefix = container.RoutePrefix,
        };

        httpContext.Features.Set<IODataFeature>(feature);
    }
}
