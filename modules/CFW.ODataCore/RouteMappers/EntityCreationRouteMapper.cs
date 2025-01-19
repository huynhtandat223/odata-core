using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Metadata;
using CFW.ODataCore.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RouteMappers;

//public class EntityModelBinder<TSource>
//{
//    private readonly JsonOptions _jsonOptions;
//    public EntityModelBinder(IOptions<JsonOptions> jsonOptions)
//    {
//        _jsonOptions = jsonOptions.Value;
//    }

//    public async Task<Result<TSource>> Bind(HttpContext httpContext, MetadataEntity metadata)
//    {
//        //TODO: handle binding errors
//        if (metadata.ConfigurationType is null)
//        {
//            var noConfigModel = await JsonSerializer
//            .DeserializeAsync<TSource>(httpContext.Request.BodyReader.AsStream()
//            , _jsonOptions.JsonSerializerOptions);

//            return noConfigModel.Success();
//        }

//        var converter = new EntityDeltaConverter<TSource>(metadata);
//        using var bodyStream = httpContext.Request.BodyReader.AsStream();
//        _jsonOptions.JsonSerializerOptions.Converters.Add(converter);

//        var model = await JsonSerializer
//            .DeserializeAsync<EntityDelta<TSource>>(bodyStream, _jsonOptions.JsonSerializerOptions);


//        throw new NotImplementedException();
//        //return model.Success();
//    }
//}

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
            , EntityDelta<TSource> delta
            , [FromServices] IEntityCreationHandler<TSource> entityCreationHandler
            , CancellationToken cancellationToken) =>
        {
            var command = new CreationCommand<TSource>(delta, _metadata);
            var result = await entityCreationHandler.Handle(command, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }
}

