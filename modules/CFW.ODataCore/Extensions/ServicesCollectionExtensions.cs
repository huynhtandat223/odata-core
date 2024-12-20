using CFW.ODataCore.EFCore;
using CFW.ODataCore.Features.BoundActions;
using CFW.ODataCore.Handlers;
using CFW.ODataCore.OData;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Extensions;

public static class ServicesCollectionExtensions
{
    public static IServiceCollection AddGenericDbContext<TDbContext>(this IServiceCollection services
        , ServiceLifetime dbServiceLifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        var contextProvider = typeof(ContextProvider<>).MakeGenericType(typeof(TDbContext));
        services.Add(new ServiceDescriptor(typeof(IODataDbContextProvider), contextProvider, dbServiceLifetime));

        return services;
    }

    public static IMvcBuilder AddGenericODataEndpoints(this IMvcBuilder mvcBuilder, BaseODataTypeResolver? oDataTypeResolver = null)
    {
        oDataTypeResolver ??= new ODataTypeResolver(Constants.DefaultODataRoutePrefix);
        var services = mvcBuilder.Services;
        var routeRefixes = oDataTypeResolver.GetRoutePrefixes();
        foreach (var routePrefix in routeRefixes)
        {
            var container = ODataContainerCollection.Instance.AddOrGetContainer(routePrefix!);
            container.AddEntitySets(routePrefix, oDataTypeResolver);
        }

        services.AddScoped(typeof(IRequestHandler<,>), typeof(DefaultRequestHandler<,>));
        services.AddScoped(typeof(IQueryHandler<,>), typeof(DefaultQueryHandler<,>));
        services.AddScoped(typeof(IGetByKeyHandler<,>), typeof(DefaultGetByKeyHandler<,>));
        services.AddScoped(typeof(ICreateHandler<,>), typeof(DefaultCreateHandler<,>));
        services.AddScoped(typeof(IDeleteHandler<,>), typeof(DefaultDeleteHandler<,>));
        services.AddScoped(typeof(IPatchHandler<,>), typeof(DefaultPatchHandler<,>));

        services.AddScoped(typeof(IBoundActionRequestHandler<,,,>), typeof(DefaultBoundActionRequestHandler<,,,>));
        foreach (var container in ODataContainerCollection.Instance.MetadataContainers)
        {
            var boundActionMetadata = container.EntityMetadataList
                .SelectMany(x => x.BoundActionMetadataList)
                .ToList();
            foreach (var metadata in boundActionMetadata)
            {
                var serviceType = metadata.ResponseType == typeof(Result)
                    ? typeof(IODataActionHandler<>).MakeGenericType(metadata.RequestType)
                    : typeof(IODataActionHandler<,>).MakeGenericType(metadata.RequestType, metadata.ResponseType);

                services.AddScoped(serviceType, metadata.HandlerType);
            }

        }
        return ODataContainerCollection.Instance.Build(mvcBuilder);

    }

    public static void UseGenericODataEndpoints(this WebApplication app)
    {
        if (ODataContainerCollection.Instance.MetadataContainers.Any())
        {

            app.UseRouting();
            app.MapControllers();
        }
    }
}
