using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData;
using System.Text;

namespace CFW.ODataCore.RouteMappers;

public class EntityGetByKeyRouteMapper<TSource> : IRouteMapper
    where TSource : class
{

    private readonly IRouteMapper _internalRouteMapper;
    public EntityGetByKeyRouteMapper(MetadataEntity metadata, IServiceScopeFactory serviceScopeFactory)
    {
        if (metadata.KeyProperty is null)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dbContextProvider = scope.ServiceProvider.GetRequiredService<IDbContextProvider>();
            var db = dbContextProvider.GetDbContext();
            metadata.InitSourceMetadata(db);
        }

        var internalRouteMapperType = typeof(EntityGetByKeyRouteMapper<,>)
            .MakeGenericType(typeof(TSource), metadata.KeyProperty!.PropertyInfo!.PropertyType);
        _internalRouteMapper = (IRouteMapper)Activator.CreateInstance(internalRouteMapperType, metadata)!;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        return _internalRouteMapper.MapRoutes(routeGroupBuilder);
    }
}

public class EntityGetByKeyRouteMapper<TSource, TKey> : IRouteMapper
    where TSource : class
{
    private readonly MetadataEntity _metadata;

    public EntityGetByKeyRouteMapper(MetadataEntity metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        var ignoreQueryOptions = _metadata.ODataQueryOptions.IgnoreQueryOptions;
        var formatter = new ODataOutputFormatter([ODataPayloadKind.ResourceSet]);
        formatter.SupportedEncodings.Add(Encoding.UTF8);

        var routePattern = _metadata.GetKeyPattern();

        routeGroupBuilder.MapGet(routePattern, async (HttpContext httpContext
                , TKey key
                , CancellationToken cancellationToken) =>
        {
            var dbContextProvider = httpContext.RequestServices.GetRequiredService<IDbContextProvider>();
            var db = dbContextProvider.GetDbContext();

            var queryable = db.Set<TSource>().AsNoTracking();

            var feature = _metadata.CreateOrGetODataFeature<TSource>();
            var predicate = _metadata.BuilderEqualExpression(db.Set<TSource>(), key);

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

        }).WithName(_metadata.Name);

        return Task.CompletedTask;
    }
}
