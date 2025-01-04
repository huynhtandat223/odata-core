using CFW.ODataCore.Attributes;
using CFW.ODataCore.DefaultHandlers;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CFW.ODataCore.ODataMetadata;

public class EntityCRUDRoutingMetadata
{
    public required Type EntityType { set; get; }

    public required Type DbType { set; get; }

    public required Type KeyType { set; get; }

    public required string Name { set; get; }

    public required bool TargetTypeIsHandler { set; get; }

    public Dictionary<EntityMethod, ServiceDescriptor> ServiceDescriptors { set; get; }
        = new Dictionary<EntityMethod, ServiceDescriptor>();

    public Dictionary<EntityMethod, IAuthorizeData> AuthorizeDataList { set; get; }
        = new Dictionary<EntityMethod, IAuthorizeData>();

    internal static EntityCRUDRoutingMetadata Create(Type targetType, EntityAttribute entityAttribute)
    {
        var entityType = targetType;
        var setupMethods = entityAttribute.Methods;
        var interfaces = targetType.GetInterfaces().Where(x => x.IsGenericType);

        var authorizes = targetType.GetCustomAttributes<EntityAuthorizeAttribute>().ToArray();
        var authorizeDataList = new Dictionary<EntityMethod, IAuthorizeData>();

        var implementationInterfaces = interfaces
            .Where(x => _supportHandlers.Values.Contains(x.GetGenericTypeDefinition()))
            .ToList();

        var targetTypeIsHandler = implementationInterfaces.Any();
        if (targetTypeIsHandler && setupMethods!.Any())
            throw new InvalidOperationException("Only set Method values for Entity class");

        //Add all methods as default if apply type is viewModel and no method set
        IEnumerable<EntityMethod> availableMethods = entityAttribute.Methods;
        if (!targetTypeIsHandler && !availableMethods.Any())
            availableMethods = _supportHandlers.Keys;

        //Set support methods in case of handler
        if (targetTypeIsHandler)
        {
            var handlerDefinitionTypes = implementationInterfaces
            .Select(x => x.GetGenericTypeDefinition())
            .ToList();
            var handlerSupportMethods = _supportHandlers.Where(x => handlerDefinitionTypes.Contains(x.Value.GetGenericTypeDefinition()))
                .Select(x => x.Key);

            availableMethods = handlerSupportMethods;

            var viewModelArgTypes = implementationInterfaces
                .Select(x => x.GetGenericArguments().First());

            //Handler must implement only one entity type
            if (viewModelArgTypes.Count() > 1)
            {
                var duplication = viewModelArgTypes
                    .GroupBy(x => x)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToList();

                throw new InvalidOperationException($"Duplicate handler types: {string.Join(", ", duplication)}");
            }

            entityType = viewModelArgTypes.Single();
        }

        //authorize data
        if (authorizes.Any())
        {
            var authorizeDataPerMethod = authorizes
                .SelectMany(entityAttr => (entityAttr.ApplyMethods)
                    .Select(method => new { EntityAttr = entityAttr, Method = method }))
                .Where(x => availableMethods.Contains(x.Method))
                .GroupBy(x => x.Method)
                .ToList();

            if (authorizeDataPerMethod.Any(m => m.Count() > 1))
            {
                var duplication = authorizeDataPerMethod
                    .Where(m => m.Count() > 1)
                    .Select(m => m.Key)
                    .First();
                throw new InvalidOperationException($"{duplication} has multiple authorizes");
            }

            authorizeDataList = authorizeDataPerMethod
                .ToDictionary(x => x.Key, x => (IAuthorizeData)x.Single().EntityAttr);
        }

        //Get key property
        var keyProp = entityType.GetProperties()
                    .SingleOrDefault(x => x.GetCustomAttribute<KeyAttribute>() is not null);

        if (keyProp is null)
            keyProp = entityType.GetProperty("Id");

        if (keyProp is null)
            keyProp = entityType.GetProperty(entityType.Name + "Id");

        if (keyProp is null)
            throw new Exception($"Key property not found for {entityType}");
        var keyType = keyProp.PropertyType;

        var serviceDescriptors = new List<ServiceDescriptor>();
        var metadata = new EntityCRUDRoutingMetadata
        {
            EntityType = entityType,
            DbType = entityAttribute.DbType ?? entityType,
            KeyType = keyType,
            Name = entityAttribute.Name,
            TargetTypeIsHandler = targetTypeIsHandler,
            AuthorizeDataList = authorizeDataList
        };

        Type? serviceType = null, implementationType = null;
        Type dbType = entityAttribute.DbType
            ?? entityType
            ?? throw new ArgumentNullException(nameof(dbType));

        foreach (var availableMethod in availableMethods)
        {
            if (availableMethod == EntityMethod.Query)
            {
                serviceType = typeof(IEntityQueryHandler<>).MakeGenericType(entityType);
                implementationType = targetTypeIsHandler
                    ? targetType
                    : dbType is null
                        ? typeof(EntityQueryDefaultHandler<>).MakeGenericType(entityType)
                        : typeof(EntityQueryDefaultHandler<,>).MakeGenericType(entityType, dbType);
            }

            if (availableMethod == EntityMethod.Post)
            {
                serviceType = typeof(IEntityCreateHandler<>).MakeGenericType(entityType);
                implementationType = targetTypeIsHandler
                    ? targetType
                    : typeof(EntityCreateDefaultHandler<,>).MakeGenericType(entityType, dbType!);
            }

            if (availableMethod == EntityMethod.GetByKey)
            {
                serviceType = typeof(IEntityGetByKeyHandler<,>).MakeGenericType(entityType, keyType);
                implementationType = targetTypeIsHandler
                    ? targetType
                    : typeof(EntityGetByKeyDefaultHandler<,>).MakeGenericType(entityType, keyType);
            }

            if (availableMethod == EntityMethod.Patch)
            {
                serviceType = typeof(IEntityPatchHandler<,>).MakeGenericType(entityType, keyType);
                implementationType = targetTypeIsHandler
                    ? targetType
                    : typeof(EntityPatchDefaultHandler<,,>).MakeGenericType(entityType, dbType!, keyType);
            }

            if (availableMethod == EntityMethod.Delete)
            {
                serviceType = typeof(IEntityDeleteHandler<,>).MakeGenericType(entityType, keyType);
                implementationType = targetTypeIsHandler
                    ? targetType
                    : typeof(EntityDeleteDefaultHandler<,>).MakeGenericType(entityType, keyType);
            }

            if (serviceType is not null && implementationType is not null)
            {
                var serviceDescriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
                metadata.ServiceDescriptors.Add(availableMethod, serviceDescriptor);
            }
        }

        return metadata;
    }

    private static Dictionary<EntityMethod, Type> _supportHandlers = new Dictionary<EntityMethod, Type>
    {
        { EntityMethod.Post, typeof(IEntityCreateHandler<>) },
        { EntityMethod.Query, typeof(IEntityQueryHandler<>) },
        { EntityMethod.GetByKey, typeof(IEntityGetByKeyHandler<,>) },
        { EntityMethod.Patch, typeof(IEntityPatchHandler<,>) },
        { EntityMethod.Delete, typeof(IEntityDeleteHandler<,>) }
    };
}


