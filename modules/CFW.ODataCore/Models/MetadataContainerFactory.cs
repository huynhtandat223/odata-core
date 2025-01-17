using CFW.ODataCore.Attributes;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Metadata;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.Models;

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
            .SelectMany(x => x.GetCustomAttributes<EntityAttribute>()
            .Aggregate(new List<EntityAttribute>(), (list, attr) =>
            {
                attr.TargetType = x;
                attr.RoutePrefix = attr.RoutePrefix ?? sanitizedRoutePrefix;
                list.Add(attr);
                return list;
            }));

        var containers = new List<MetadataContainer>();
        foreach (var containerGroup in entityEndpoinConfigs.GroupBy(x => x.RoutePrefix))
        {
            var metadataContainer = new MetadataContainer(containerGroup.Key!, mimimalApiOptions);

            var entityEndpoints = containerGroup.SelectMany(x => x.Methods!.Select(m => new { Attribute = x, Method = m }))
                .GroupBy(x => new { x.Attribute.Name, x.Attribute.TargetType, x.Attribute.AllowedQueryOptions });

            foreach (var key in entityEndpoints)
            {
                var duplicateMethods = key.GroupBy(x => x.Method)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToArray();
                if (duplicateMethods.Any())
                    throw new InvalidOperationException($"Duplicate methods {string.Join(",", duplicateMethods)} for entity {key.Key.Name}");

                var endpoint = key.Key.Name;
                var sourceType = key.Key.TargetType!;
                var allowedQueryOptions = key.Key.AllowedQueryOptions;

                var metadataEntity = new MetadataEntity
                {
                    Name = endpoint,
                    Methods = key.Select(x => x.Method).ToArray(),
                    SourceType = sourceType,
                    Container = metadataContainer,
                    ODataQueryOptions = new ODataQueryOptions { InternalAllowedQueryOptions = allowedQueryOptions }
                };
                metadataContainer.MetadataEntities.Add(metadataEntity);
            }
            containers.Add(metadataContainer);
        }

        var operationHandlerTypes = new[] { typeof(IOperationHandler<>), typeof(IOperationHandler<,>) };
        var boundOperationConfigs = CacheType
            .SelectMany(x => x.GetCustomAttributes<EntityActionAttribute>()
            .Aggregate(new List<MetadataEntityAction>(), (list, attr) =>
            {
                if (x.IsAbstract)
                    return list;

                var interfaces = x.GetInterfaces().Where(i => i.IsGenericType
                    && operationHandlerTypes.Contains(i.GetGenericTypeDefinition()));

                if (!interfaces.Any())
                    throw new InvalidOperationException($"Entity action {attr.ActionName} " +
                        $"handler {x.FullName} not implement any operation interface");

                if (interfaces.Count() > 1)
                    throw new InvalidOperationException($"Entity action {attr.ActionName} " +
                        $"handler {x.FullName} implement multiple operation interface");

                var metadata = new MetadataEntityAction
                {
                    ImplementedInterface = interfaces.First(),
                    RoutePrefix = attr.RoutePrefix ?? sanitizedRoutePrefix,
                    TargetType = x,
                    ActionName = attr.ActionName,
                    HttpMethod = attr.HttpMethod,
                    BoundEntityType = attr.BoundEntityType,
                    EntityName = attr.EntityName
                };

                list.Add(metadata);
                return list;
            }));
        foreach (var containerGroup in boundOperationConfigs.GroupBy(x => x.RoutePrefix))
        {
            var container = containers.FirstOrDefault(x => x.RoutePrefix == containerGroup.Key);
            if (container is null)
                container = new MetadataContainer(containerGroup.Key!, mimimalApiOptions);

            var entityOperations = containerGroup
                .GroupBy(x => new { x.EntityName, x.BoundEntityType, x.TargetType, x.ActionName });
            foreach (var entityOperationMetadata in entityOperations)
            {
                if (entityOperationMetadata.Count() > 1)
                    throw new NotImplementedException($"Duplicate entity operation {entityOperationMetadata.Key.ActionName} for entity {entityOperationMetadata.Key.BoundEntityType}");
                var entityActionMetadataItem = entityOperationMetadata.Single();

                var entityName = entityOperationMetadata.Key.EntityName;
                var boundEntityType = entityOperationMetadata.Key.BoundEntityType;

                var entityMetadataList = container.MetadataEntities
                    .Where(x => x.SourceType == boundEntityType);
                MetadataEntity? boundedEntityMetadata = null;
                if (entityMetadataList.Any())
                {
                    if (entityMetadataList.Count() > 1 && entityName.IsNullOrWhiteSpace())
                        throw new InvalidOperationException($"Entity {entityOperationMetadata.Key.BoundEntityType} has " +
                            $"multiple entity attribute apply on it, please specify entity name");

                    boundedEntityMetadata = entityMetadataList.SingleOrDefault(x => x.Name == entityName);

                    if (boundedEntityMetadata is null)
                        throw new InvalidOperationException($"Entity {entityOperationMetadata.Key.BoundEntityType} has " +
                            $"no entity attribute with name {entityName}");
                }

                if (boundedEntityMetadata is null)
                    throw new InvalidOperationException($"Entity {entityOperationMetadata.Key.BoundEntityType} has " +
                        $"no entity attribute apply on it");

                boundedEntityMetadata.Operations.Add(entityActionMetadataItem);
            }
        }

        var unboundOperationConfigs = CacheType
            .SelectMany(x => x.GetCustomAttributes<UnboundActionAttribute>()
            .Aggregate(new List<MetadataUnboundAction>(), (list, attr) =>
            {
                if (x.IsAbstract)
                    return list;

                var interfaces = x.GetInterfaces().Where(i => i.IsGenericType
                    && operationHandlerTypes.Contains(i.GetGenericTypeDefinition()));
                if (!interfaces.Any())
                    throw new InvalidOperationException($"Unbound action {attr.ActionName} " +
                        $"handler {x.FullName} not implement any operation interface");

                if (interfaces.Count() > 1)
                    throw new InvalidOperationException($"Unbound action {attr.ActionName} " +
                        $"handler {x.FullName} implement multiple operation interface");

                var metadata = new MetadataUnboundAction
                {
                    ImplementedInterface = interfaces.First(),
                    RoutePrefix = attr.RoutePrefix ?? sanitizedRoutePrefix,
                    TargetType = x,
                    ActionName = attr.ActionName,
                    HttpMethod = attr.ActionMethod
                };
                list.Add(metadata);
                return list;
            }));
        foreach (var containerGroup in unboundOperationConfigs.GroupBy(x => x.RoutePrefix))
        {
            var container = containers.FirstOrDefault(x => x.RoutePrefix == containerGroup.Key);
            if (container is null)
                container = new MetadataContainer(containerGroup.Key!, mimimalApiOptions);

            var unboundOperations = containerGroup
                .GroupBy(x => new { x.TargetType, x.ActionName });

            foreach (var unboundOperationMetadata in unboundOperations)
            {
                if (unboundOperationMetadata.Count() > 1)
                    throw new NotImplementedException($"Duplicate unbound operation {unboundOperationMetadata.Key.ActionName}");

                var unboundActionMetadataItem = unboundOperationMetadata.Single();

                container.UnboundOperations.Add(unboundActionMetadataItem);
            }
        }

        return containers;
    }
}
