using CFW.ODataCore.Core;
using CFW.ODataCore.Handlers;
using System.Reflection;

namespace CFW.ODataCore;

public static class ServicesCollectionExtensions
{
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
                ODataType = x, //can be ViewModel or Handler
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

        services.AddScoped(typeof(ApiHandler<,>));

        var queryType = typeof(IQueryHandler<,>);
        var queryTypes = includedAssemblies.SelectMany(x => x.GetTypes())
            .Where(x => x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == queryType))
            .ToList();
        foreach (var qType in queryTypes)
        {
            var intefaceType = qType.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == queryType);
            services.AddScoped(intefaceType, qType);
        }

        return ODataContainerCollection.Instance.Build(services);
    }

    public static void UseGenericODataEndpoints(this WebApplication app)
    {
        app.UseRouting();
        app.MapControllers();
    }
}
