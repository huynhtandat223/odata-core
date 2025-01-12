using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.RequestHandlers;

public class EntityQueryRequestHandler<TSource, TViewModel, TKey> : IHttpRequestHandler
{
    private readonly EntityMetadata<TSource, TViewModel, TKey> _entityMetadata;

    public EntityQueryRequestHandler(EntityMetadata<TSource, TViewModel, TKey> entityMetadata)
    {
        _entityMetadata = entityMetadata;
    }

    public Task MappRouters(WebApplication app)
    {
        var entityGroup = _entityMetadata.Container.CreateOrGetEntityGroup(app, _entityMetadata);
        var endpoint = _entityMetadata.EntityEndpoint;

        var entitySourceFactory = endpoint.EntityQueryableFactory;
        if (entitySourceFactory is null)
            throw new InvalidOperationException("EntitySourceFactory must be set");

        var ignoreQueryOptions = _entityMetadata.IgnoreQueryOptions;
        var viewModelSelector = _entityMetadata.ViewModelSelector;

        entityGroup.MapGet($"/", (HttpContext httpContext
        , CancellationToken cancellationToken) =>
        {
            httpContext.Features.Set(_entityMetadata.Feature);
            var odataQueryContext = new ODataQueryContext(_entityMetadata.Feature.Model, typeof(TViewModel)
                , _entityMetadata.Feature.Path);
            var options = new ODataQueryOptions<TViewModel>(odataQueryContext, httpContext.Request);

            var queryable = entitySourceFactory(httpContext.RequestServices);
            var viewModelQueryable = queryable.Select(viewModelSelector);

            var result = options.ApplyTo(viewModelQueryable, ignoreQueryOptions);

            return result.Success().ToODataResults();

        }).Produces<TViewModel>();

        return Task.CompletedTask;
    }
}

