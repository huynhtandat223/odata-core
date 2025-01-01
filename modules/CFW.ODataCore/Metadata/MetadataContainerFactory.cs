using CFW.ODataCore.Attributes;
using CFW.ODataCore.Models;
using System.Reflection;

namespace CFW.ODataCore.Core;

public class MetadataContainerFactory
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttributes<ODataRoutingAttribute>().Any())
        .ToList();

    public IEnumerable<Type> CacheType { protected set; get; } = _cachedType;

    public IEnumerable<ODataMetadataContainer> CreateContainers(IServiceCollection services, string defaultRoutePrefix)
    {
        var routingAttributes = CacheType
            .SelectMany(x => x.GetCustomAttributes<ODataRoutingAttribute>()
                .Select(attr => new { TargetType = x, RoutingAttribute = attr }))
            .GroupBy(x => x.RoutingAttribute.RoutePrefix ?? defaultRoutePrefix)
            .ToDictionary(x => new ODataMetadataContainer(x.Key), x => x.ToList());

        foreach (var (container, routingInfoInContainer) in routingAttributes)
        {
            routingInfoInContainer
                .Where(x => x.RoutingAttribute is EntityAttribute)
                .Aggregate(container, (currentContainer, x) =>
                {
                    currentContainer.CreateOrEditEntityMetadata(x.TargetType, (EntityAttribute)x.RoutingAttribute);
                    return currentContainer;
                });

            var t = routingInfoInContainer
               .Where(x => x.RoutingAttribute is BoundOperationAttribute).ToList();

            routingInfoInContainer
                .Where(x => x.RoutingAttribute is BoundOperationAttribute)
                .Aggregate(container, (currentContainer, x) =>
                {
                    currentContainer.CreateEntityOpration(x.TargetType, (BoundOperationAttribute)x.RoutingAttribute);
                    return currentContainer;
                });

            routingInfoInContainer
                .Where(x => x.RoutingAttribute is UnboundOperationAttribute)
                .Aggregate(container, (currentContainer, x) =>
                {
                    currentContainer.CreateUnboundOperation(x.TargetType, (UnboundOperationAttribute)x.RoutingAttribute);
                    return currentContainer;
                });

            container.BuildEdmModel();

            container.RegisterRoutingServices(services);

            yield return container;
        }
    }
}
