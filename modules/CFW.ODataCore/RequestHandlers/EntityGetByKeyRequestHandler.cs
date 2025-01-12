using Microsoft.AspNetCore.OData.Query;
using System.Net;

namespace CFW.ODataCore.RequestHandlers;

public class EntityGetByKeyRequestHandler<TSource, TViewModel, TKey> : IHttpRequestHandler
{
    private readonly EntityMetadata<TSource, TViewModel, TKey> _entityMetadata;

    public EntityGetByKeyRequestHandler(EntityMetadata<TSource, TViewModel, TKey> entityMetadata)
    {
        _entityMetadata = entityMetadata;
    }

    public Task MappRouters(WebApplication app)
    {
        var entityGroup = _entityMetadata.Container.CreateOrGetEntityGroup(app, _entityMetadata);
        var endpoint = _entityMetadata.EntityEndpoint;

        var entitySourceFactory = endpoint.EntityQueryableFactory;

        var ignoreQueryOptions = _entityMetadata.IgnoreQueryOptions;
        var viewModelSelector = _entityMetadata.ViewModelSelector;

        entityGroup.MapGet("/{key}", (HttpContext httpContext, TKey key
        , CancellationToken cancellationToken) =>
        {
            httpContext.Features.Set(_entityMetadata.Feature);
            var odataQueryContext = new ODataQueryContext(_entityMetadata.Feature.Model, typeof(TViewModel)
                , _entityMetadata.Feature.Path);
            var options = new ODataQueryOptions<TViewModel>(odataQueryContext, httpContext.Request);

            var queryable = entitySourceFactory(httpContext.RequestServices);
            var equalKeyExpression = _entityMetadata.GetByKeyExpression(key);

            var viewModelQueryable = queryable
            .Select(viewModelSelector)
            .Where(equalKeyExpression);

            var appliedQuery = options.ApplyTo(viewModelQueryable, ignoreQueryOptions);
            var result = appliedQuery.Cast<object>().SingleOrDefault();

            if (result == null)
                return new Result<dynamic>
                {
                    HttpStatusCode = HttpStatusCode.NotFound,
                    IsSuccess = false,
                }.ToODataResults();

            return new Result<dynamic>
            {
                HttpStatusCode = HttpStatusCode.OK,
                IsSuccess = true,
                Data = result,
            }.ToODataResults();

        }).Produces<TViewModel>();

        return Task.CompletedTask;
    }
}
