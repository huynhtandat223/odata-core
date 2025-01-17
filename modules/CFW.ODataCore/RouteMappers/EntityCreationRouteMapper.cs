using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RouteMappers;

public class EntityCreationRouteMapper<TSource> : IRouteMapper
where TSource : class
{
    private readonly MetadataEntity _metadata;

    public EntityCreationRouteMapper(MetadataEntity metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        routeGroupBuilder.MapPost("/", async (HttpContext httpContext
            , [FromServices] IEntityCreationHandler<TSource> entityCreationHandler
            , TSource model
            , CancellationToken cancellationToken) =>
        {
            var result = await entityCreationHandler.Handle(model, cancellationToken);
            return result.ToResults();
        }).Produces<TSource>();

        return Task.CompletedTask;
    }

    //public Task MappRouters(WebApplication app)
    //{
    //    var entityGroup = _entityMetadata.Container.CreateOrGetEntityGroup(app, _entityMetadata);
    //    var endpoint = _entityMetadata.EntityEndpoint;

    //    var entityCreationFactory = endpoint.EntityCreateFactory;
    //    entityGroup.MapPost("/", async (HttpContext httpContext
    //    , CancellationToken cancellationToken) =>
    //    {
    //        var rootConverter = new JsonDeltaConverter<TSource>(
    //            _entityMetadata.EntityEndpoint.Properties.Keys.ToArray()
    //                , _entityMetadata.EntityEndpoint.NestedTypes
    //                , _entityMetadata.EntityEndpoint.CollectionTypes);

    //        var jsonSerializerOptions = new JsonSerializerOptions
    //        {
    //            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    //            Converters = { rootConverter }
    //        };

    //        foreach (var x in _entityMetadata.EntityEndpoint.NestedTypes)
    //        {
    //            var nestedConverterType = typeof(JsonDeltaConverter<>).MakeGenericType(x.Value);
    //            var properties = x.Value.GetProperties();

    //            var converter = (JsonConverter)Activator.CreateInstance(nestedConverterType
    //                , args: [properties, new Dictionary<string, Type>(), new Dictionary<string, Type>()]);
    //            jsonSerializerOptions.Converters.Add(converter);
    //        }

    //        foreach (var x in _entityMetadata.EntityEndpoint.CollectionTypes)
    //        {
    //            var nestedConverterType = typeof(JsonDeltaConverter<>).MakeGenericType(x.Value);

    //            //TODO: IMPORTAN: need to refactor now
    //            var properties = x.Value.GetProperties()
    //                .Where(x => !IsCustomType(x.PropertyType) && !IsCollectionOfCustomType(x.PropertyType)).ToArray();

    //            var converter = (JsonConverter)Activator.CreateInstance(nestedConverterType
    //                , args: [properties, new Dictionary<string, Type>(), new Dictionary<string, Type>()]);
    //            jsonSerializerOptions.Converters.Add(converter);
    //        }

    //        var viewModel = JsonSerializer.Deserialize<JsonDelta<TSource>>(httpContext.Request.BodyReader.AsStream()
    //            , jsonSerializerOptions);

    //        var result = await entityCreationFactory(httpContext.RequestServices, viewModel.ChangedProperties);

    //        return Results.Created("", result);

    //    }).Produces<TViewModel>();

    //    return Task.CompletedTask;
    //}

    //[Obsolete("Wrong business logic")]
    //public static bool IsCustomType(Type type)
    //{
    //    // Custom types are typically not part of system namespaces
    //    return type.Namespace.StartsWith("CFW.") && !type.IsEnum;
    //}

    //[Obsolete("Wrong business logic")]
    //public static bool IsCollectionOfCustomType(Type type)
    //{
    //    // Check if the type is a collection
    //    if (typeof(IEnumerable).IsAssignableFrom(type))
    //    {
    //        // For arrays
    //        if (type.IsArray)
    //        {
    //            return IsCustomType(type.GetElementType());
    //        }
    //        // For generic collections
    //        if (type.IsGenericType)
    //        {
    //            var genericArguments = type.GetGenericArguments();
    //            if (genericArguments.Length == 1)
    //            {
    //                return IsCustomType(genericArguments[0]);
    //            }
    //        }
    //    }

    //    return false;
    //}


}

