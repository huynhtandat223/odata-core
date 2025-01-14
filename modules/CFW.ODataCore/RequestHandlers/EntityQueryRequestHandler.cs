namespace CFW.ODataCore.RequestHandlers;

public class EntityQueryRequestHandler<TSource, TViewModel, TKey> : IRouteMapper
{
    private readonly MetadataEntity _metadataEntity;

    public EntityQueryRequestHandler(MetadataEntity metadataEntity)
    {
        _metadataEntity = metadataEntity;
    }

    public Task MappRoutes(RouteGroupBuilder routeGroupBuilder, WebApplication app)
    {
        routeGroupBuilder.MapGet($"/", (HttpContext httpContext
        , CancellationToken cancellationToken) =>
        {
            //var entitySourceFactory = _metadataEntity.EntityQueryableFactory;
            //var ignoreQueryOptions = _metadataEntity.IgnoreQueryOptions;
            //var viewModelSelector = _metadataEntity.ViewModelSelector;

            //httpContext.Features.Set(_metadataEntity.Feature);
            //var odataQueryContext = new ODataQueryContext(_metadataEntity.Feature.Model, typeof(TViewModel)
            //    , _metadataEntity.Feature.Path);
            //var options = new ODataQueryOptions<TViewModel>(odataQueryContext, httpContext.Request);

            //var queryable = entitySourceFactory(httpContext.RequestServices);
            //var viewModelQueryable = queryable.Select(viewModelSelector);

            //var result = options.ApplyTo(viewModelQueryable, ignoreQueryOptions);

            //return result.Success().ToODataResults();

        }).Produces<TViewModel>();

        return Task.CompletedTask;
    }
}

