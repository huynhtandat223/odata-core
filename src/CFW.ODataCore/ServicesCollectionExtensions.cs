using CFW.Core.Utils;
using CFW.ODataCore.Core;
using CFW.ODataCore.Handlers;
using System.Reflection;

namespace CFW.ODataCore;

public static class ServicesCollectionExtensions
{
    public static IServiceCollection AddGenericODataEndpoints(this IServiceCollection services
        , Assembly[] assemblies
        , string defaultRoutePrefix = "odata-api")
    {
        var odataRoutings = assemblies.SelectMany(x => x.GetTypes())
                .Where(x => x.GetCustomAttribute<ODataRoutingAttribute>() is not null)
                .Select(x => new { ViewModelType = x, RoutingInfo = x.GetCustomAttribute<ODataRoutingAttribute>()! })
                .ToList();

        if (odataRoutings.Any(x => x.RoutingInfo.Name.IsNullOrWhiteSpace()))
        {
            var invalidTypes = odataRoutings
                .Where(x => x.RoutingInfo.Name.IsNullOrWhiteSpace())
                .Select(x => new { x.ViewModelType.Name })
                .ToArray();
            var names = string.Join(separator: ',', values: invalidTypes.Select(x => x.Name));

            throw new InvalidOperationException($"Invalid routing name for {names}");
        }

        var containerGroups = odataRoutings.GroupBy(x => x.RoutingInfo.RouteRefix ?? defaultRoutePrefix);
        foreach (var containerGroup in containerGroups)
        {
            var routePrefix = containerGroup.Key;
            var container = ODataContainerCollection.Instance.AddOrGetContainer(routePrefix!);

            foreach (var routing in containerGroup)
            {
                var viewModelType = routing.ViewModelType;
                var odataInterface = viewModelType.GetInterfaces()
                    .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IODataViewModel<>));
                if (odataInterface is null)
                {
                    throw new InvalidOperationException("ViewModel must implement IODataViewModel<T>");
                }

                routing.RoutingInfo.EntityType = viewModelType;
                routing.RoutingInfo.KeyType = routing.ViewModelType.GetInterfaces()
                    .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IODataViewModel<>))
                    .Select(x => x.GetGenericArguments().First())
                    .First();
                container.AddEntitySet(routing.RoutingInfo);
            }
        }

        services.AddScoped(typeof(ApiHandler<,>));

        return ODataContainerCollection.Instance.Build(services);
    }

    public static void UseGenericODataEndpoints(this WebApplication app)
    {
        app.UseRouting();
        app.MapControllers();
    }
}
