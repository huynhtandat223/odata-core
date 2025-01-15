using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData;
using System.Text;

namespace CFW.ODataCore.RequestHandlers;

public interface IEntityQueryRequestHandler
{
    Task MappRoutes(EntityRequestContext entityRequestContext);
}

public class DefaultEntityQueryRequestHandler<TSource> : IEntityQueryRequestHandler
    where TSource : class
{
    public Task MappRoutes(EntityRequestContext entityRequestContext)
    {
        var entityMetadata = entityRequestContext.MetadataEntity;
        var ignoreQueryOptions = entityMetadata.ODataQueryOptions.IgnoreQueryOptions;

        entityRequestContext.EntityRouteGroupBuider.MapGet($"/", async (HttpContext httpContext
                , CancellationToken cancellationToken) =>
        {
            var feature = entityMetadata.CreateOrGetODataFeature<TSource>();
            httpContext.Features.Set(feature);

            var dbContextProvider = httpContext.RequestServices.GetRequiredService<IODataDbContextProvider>();
            var db = dbContextProvider.GetDbContext();
            var queryable = db.Set<TSource>().AsNoTracking();

            var odataQueryContext = new ODataQueryContext(feature.Model, typeof(TSource), feature.Path);
            var options = new ODataQueryOptions<TSource>(odataQueryContext, httpContext.Request);

            var result = options.ApplyTo(queryable, ignoreQueryOptions);

            var formatterContext = new OutputFormatterWriteContext(httpContext,
                (stream, encoding) => new StreamWriter(stream, encoding),
                result.GetType() ?? typeof(object), result)
            {
                ContentType = "application/json;odata.metadata=none",
            };

            var formatter = new ODataOutputFormatter([ODataPayloadKind.ResourceSet]);
            formatter.SupportedEncodings.Add(Encoding.UTF8);
            await formatter.WriteAsync(formatterContext);

        }).Produces<TSource>();

        return Task.CompletedTask;
    }
}

