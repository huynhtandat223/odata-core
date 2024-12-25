using CFW.ODataCore.Core.Attributes;
using System.Reflection;

namespace CFW.ODataCore.Core.MetadataResolvers;

public class MetadataContainerFactory
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttributes<EndpointAttribute>().Any())
        .ToList();

    public virtual IEnumerable<Type> CachedType => _cachedType;

    //public void ScanMetadata(string defaultRoutePrefix)
    //{
    //    var routingAttributes = _cachedType
    //        .SelectMany(x => x.GetCustomAttributes<EndpointAttribute>())
    //        .Select(x => new { ApplyType = x.GetType(), RoutingAttribute = x })
    //        .GroupBy(x => x.RoutingAttribute.RoutePrefix ?? defaultRoutePrefix)
    //        .ToDictionary(x => new ODataMetadataContainer(x.Key), x => x.ToList());

    //    foreach (var (container, group) in routingAttributes)
    //    {
    //        var entitySetMetadataList = group
    //            .Where(x => x.RoutingAttribute is EndpointEntityActionAttribute)
    //            .ToList();

    //        if (entitySetMetadataList.Any())
    //        {
    //            var entitySetMetadataList = new EntitySetMetadataFactory()
    //                .CreateEntitySetMetadata(entitySetMetadataList);
    //        }
    //    }
    //}


    //public IEnumerable<ODataMetadataContainer> CreateContainers()
    //{
    //    var containers = new List<ODataMetadataContainer>();
    //    var routePrefixes = _cachedType
    //        .SelectMany(x => x.GetCustomAttributes<EndpointEntityActionAttribute>().Select(c => new { ApplyType = x, RoutingAttribute = c }))
    //        .GroupBy(x => x.RoutingAttribute!.RoutePrefix ?? _defaultPrefix);

    //    //Register EntitySets
    //    foreach (var group in routePrefixes)
    //    {
    //        var routePrefix = group.Key;
    //        var container = new ODataMetadataContainer(routePrefix);

    //        var newEntitySetMetadataList = group
    //            .Select(x => CreateEntitySetMetadata(x.ApplyType, x.RoutingAttribute, container))
    //            .Where(x => x is not null)
    //            .Select(x => x!)
    //            .ToList();

    //        container.EntitySetMetadataList.AddRange(newEntitySetMetadataList);
    //    }

    //    //register bound operations
    //    var boundOperationByGroup = CachedType
    //        .Where(x => x.GetCustomAttributes<EntityOperationAttribute>().Any())
    //        .SelectMany(x => x.GetCustomAttributes<EntityOperationAttribute>()
    //            .Select(attr => new { HandlerType = x, RoutingAttribute = attr }))
    //        .GroupBy(x => x.RoutingAttribute!.RoutePrefix ?? _defaultPrefix);

    //    foreach (var group in boundOperationByGroup)
    //    {
    //        var routePrefix = group.Key;
    //        var container = containers.FirstOrDefault(x => x.RoutePrefix == routePrefix);
    //        if (container is null)
    //        {
    //            container = new ODataMetadataContainer(routePrefix);
    //            containers.Add(container);
    //        }

    //        var entitySetGroup = group.GroupBy(x => new { x.RoutingAttribute.ViewModelType, x.RoutingAttribute.KeyType });
    //        foreach (var entitySetInfo in entitySetGroup)
    //        {
    //            var entitySetMetadata = container.EntitySetMetadataList
    //                .SingleOrDefault(x => x.Container.RoutePrefix == routePrefix && x.ViewModelType == entitySetInfo.Key.ViewModelType
    //                    && x.KeyType == entitySetInfo.Key.KeyType);

    //            if (entitySetMetadata is null)
    //                throw new InvalidOperationException($"EntitySetMetadata not found for {entitySetInfo.Key.ViewModelType} and {entitySetInfo.Key.KeyType}");

    //            var boundOperationMetadataListOfEntitySet = entitySetInfo
    //                .Select(x => CreateBoundOperationMetadata(entitySetMetadata, x.HandlerType, x.RoutingAttribute));

    //            container.BoundOperationMetadataList.AddRange(boundOperationMetadataListOfEntitySet);
    //        }
    //    }

    //    //Register unbound operations
    //    var unboudOperationMetadataGroup = CachedType
    //        .Where(x => x.GetCustomAttributes<UnboundOperationAttribute>().Any())
    //        .SelectMany(x => x.GetCustomAttributes<UnboundOperationAttribute>()
    //            .Select(attr => new { HandlerType = x, RoutingAttribute = attr }))
    //        .GroupBy(x => x.RoutingAttribute!.RoutePrefix ?? _defaultPrefix);
    //    foreach (var group in unboudOperationMetadataGroup)
    //    {
    //        var routePrefix = group.Key;
    //        var container = containers.FirstOrDefault(x => x.RoutePrefix == routePrefix);
    //        if (container is null)
    //        {
    //            container = new ODataMetadataContainer(routePrefix);
    //            containers.Add(container);
    //        }

    //        var unboundOperationMetadataList = group
    //            .Select(x => CreateUnboundMetadata(container, x.HandlerType, x.RoutingAttribute))
    //            .ToList();
    //        container.UnBoundOperationMetadataList.AddRange(unboundOperationMetadataList);
    //    }

    //    return containers;
    //}
}
