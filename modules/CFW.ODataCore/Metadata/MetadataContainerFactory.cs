using CFW.ODataCore.Attributes;
using CFW.ODataCore.Models;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.Core;

public class MetadataContainerFactory : IAssemblyResolver
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttributes<BaseRoutingAttribute>().Any())
        .ToList();

    public IEnumerable<Type> CacheType { protected set; get; } = _cachedType;

    public IEnumerable<Assembly> Assemblies => _cachedType.Select(x => x.Assembly).Distinct();

    public IEnumerable<MetadataContainer> CreateMetadataContainers(string sanitizedRoutePrefix
        , EntityMimimalApiOptions mimimalApiOptions)
    {
        var entityEndpoinConfigs = CacheType
            .SelectMany(x => x.GetCustomAttributes<EntityV2Attribute>()
            .Aggregate(new List<EntityV2Attribute>(), (list, attr) =>
            {
                attr.TargetType = x;
                attr.RoutePrefix = attr.RoutePrefix ?? sanitizedRoutePrefix;
                list.Add(attr);
                return list;
            }));

        foreach (var containerGroup in entityEndpoinConfigs.GroupBy(x => x.RoutePrefix))
        {
            var metadataContainer = new MetadataContainer(containerGroup.Key!, mimimalApiOptions);

            var entityEndpoints = containerGroup.SelectMany(x => x.Methods.Select(m => new { Attribute = x, Method = m }))
                .GroupBy(x => new { x.Attribute.Name, x.Attribute.TargetType });

            foreach (var key in entityEndpoints)
            {
                var duplicateMethods = key.GroupBy(x => x.Method)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToArray();
                if (duplicateMethods.Any())
                {
                    throw new InvalidOperationException($"Duplicate methods {string.Join(",", duplicateMethods)} for entity {key.Key.Name}");
                }

                var endpoint = key.Key.Name;
                var sourceType = key.Key.TargetType!;

                var metadataEntity = new MetadataEntity
                {
                    Name = endpoint,
                    Methods = key.Select(x => x.Method).ToArray(),
                    SourceType = sourceType,
                    Container = metadataContainer
                };
                metadataContainer.MetadataEntities.Add(metadataEntity);
            }

            yield return metadataContainer;
        }
    }

    public IEnumerable<ODataMetadataContainer> CreateContainers(IServiceCollection services
        , string defaultRoutePrefix, EntityMimimalApiOptions coreOptions)
    {
        var routingAttributes = CacheType
            .SelectMany(x => x.GetCustomAttributes<BaseRoutingAttribute>()
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

            routingInfoInContainer
               .Where(x => x.RoutingAttribute is ConfigurableEntityAttribute)
               .Aggregate(container, (currentContainer, x) =>
               {
                   currentContainer.CreateDynamicEntityMetadata(x.TargetType, (ConfigurableEntityAttribute)x.RoutingAttribute);
                   return currentContainer;
               });

            container.BuildEdmModel(coreOptions);

            container.RegisterRoutingServices(services);

            yield return container;
        }
    }
}
