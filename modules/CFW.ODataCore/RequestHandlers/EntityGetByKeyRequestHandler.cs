using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData;
using System.Text;

namespace CFW.ODataCore.RequestHandlers;

public interface IEntityGetByKeyRequestHandler
{
    Task MappRoutes(EntityRequestContext entityRequestContext);
}

public class DefaultEntityGetByKeyRequestHandler<TSource, TKey> : IEntityGetByKeyRequestHandler
    where TSource : class
{
    public Task MappRoutes(EntityRequestContext entityRequestContext)
    {
        var entityMetadata = entityRequestContext.MetadataEntity;
        var ignoreQueryOptions = entityMetadata.ODataQueryOptions.IgnoreQueryOptions;
        var formatter = new ODataOutputFormatter([ODataPayloadKind.ResourceSet]);
        formatter.SupportedEncodings.Add(Encoding.UTF8);

        var routePattern = entityMetadata.GetKeyPattern();

        entityRequestContext.EntityRouteGroupBuider.MapGet(routePattern, async (HttpContext httpContext
                , TKey key
                , CancellationToken cancellationToken) =>
        {
            var dbContextProvider = httpContext.RequestServices.GetRequiredService<IODataDbContextProvider>();
            var db = dbContextProvider.GetDbContext();

            var queryable = db.Set<TSource>().AsNoTracking();

            var feature = entityMetadata.CreateOrGetODataFeature<TSource>();
            var predicate = entityMetadata.BuilderEqualExpression(db.Set<TSource>(), key!);

            httpContext.Features.Set(feature);

            queryable = queryable.Where(predicate);

            //apply query options
            var odataQueryContext = new ODataQueryContext(feature.Model, typeof(TSource), feature.Path);
            var options = new ODataQueryOptions<TSource>(odataQueryContext, httpContext.Request);
            var appliedQuery = options.ApplyTo(queryable, ignoreQueryOptions);

            var result = appliedQuery.Cast<object>().SingleOrDefault();

            //write response
            var formatterContext = new OutputFormatterWriteContext(httpContext,
                (stream, encoding) => new StreamWriter(stream, encoding),
                typeof(object), result)
            {
                ContentType = "application/json;odata.metadata=none",
            };

            await formatter.WriteAsync(formatterContext);

        }).WithName(entityMetadata.Name);

        return Task.CompletedTask;
    }
}
