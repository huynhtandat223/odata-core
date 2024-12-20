using CFW.ODataCore.EFCore;
using CFW.ODataCore.Handlers;
using CFW.ODataCore.OData;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CFW.ODataCore.Extensions;

public static class ServicesCollectionExtensions
{
    public static IServiceCollection AddAutoPopuplateEntities<TDbContext>(this IServiceCollection services
        , ServiceLifetime dbServiceLifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        var modelCustomizer = typeof(ODataModelCustomizer<>).MakeGenericType(typeof(TDbContext));

        var contextProvider = typeof(ContextProvider<>).MakeGenericType(typeof(TDbContext));
        services.Add(new ServiceDescriptor(typeof(IODataDbContextProvider), contextProvider, dbServiceLifetime));

        return services;
    }

    public static IServiceCollection AddGenericODataEndpoints(this IServiceCollection services
        , Assembly[]? assemblies = null
        , string defaultRoutePrefix = "odata-api")
    {
        var includedAssemblies = assemblies?.ToList();
        if (includedAssemblies is null)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            var rootNameSpace = callingAssembly.FullName?.Split('.')[0];
            rootNameSpace = $"{rootNameSpace}.";

            includedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.FullName?.StartsWith(rootNameSpace) == true)
                .ToList();
        }

        var odataRoutings = includedAssemblies
            .Distinct()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetCustomAttribute<ODataRoutingAttribute>() is not null)
            .Select(x => new
            {
                ODataType = x,
                x.GetCustomAttribute<ODataRoutingAttribute>()!.RouteRefix,
            })
            .ToList();

        var containerGroups = odataRoutings.GroupBy(x => x.RouteRefix ?? defaultRoutePrefix);
        foreach (var containerGroup in containerGroups)
        {
            var routePrefix = containerGroup.Key;
            var container = ODataContainerCollection.Instance.AddOrGetContainer(routePrefix!);

            var odataTypes = containerGroup.Select(x => x.ODataType).ToList();
            container.AddEntitySets(odataTypes);
        }

        services.AddScoped(typeof(IQueryHandler<,>), typeof(DefaultQueryHandler<,>));
        services.AddScoped(typeof(IGetByKeyHandler<,>), typeof(DefaultGetByKeyHandler<,>));
        services.AddScoped(typeof(ICreateHandler<,>), typeof(DefaultCreateHandler<,>));
        services.AddScoped(typeof(IDeleteHandler<,>), typeof(DefaultDeleteHandler<,>));
        services.AddScoped(typeof(IPatchHandler<,>), typeof(DefaultPatchHandler<,>));

        return ODataContainerCollection.Instance.Build(services);
    }

    public static void UseGenericODataEndpoints(this WebApplication app)
    {
        app.UseRouting();
        app.MapControllers();
    }
}
