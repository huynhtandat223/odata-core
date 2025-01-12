using CFW.ODataCore.Attributes;
using CFW.ODataCore.ODataMetadata;
using CFW.ODataCore.RequestHandlers;
using Microsoft.AspNetCore.OData.Edm;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;
using System.Reflection.Emit;

namespace CFW.ODataCore.Models;

[Obsolete]
public class ODataMetadataContainer
{
    public string RoutePrefix { get; }
    private AssemblyBuilder _assemblyBuilder;
    private ModuleBuilder _moduleBuilder;

    public ODataMetadataContainer(string routePrefix)
    {
        RoutePrefix = routePrefix;

        _assemblyBuilder = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName($"CFW.ODataCore.Dynamic.{routePrefix.ToPascalCase()}")
            , AssemblyBuilderAccess.Run);

        _moduleBuilder = _assemblyBuilder.DefineDynamicModule("MainModule");
    }

    private IEdmModel? _edmModel;
    public IEdmModel EdmModel
    {
        get
        {
            if (_edmModel is null)
                throw new InvalidOperationException("Edm model not build yet");
            return _edmModel;
        }
    }

    private RouteGroupBuilder? _routeGroupBuilder;
    internal RouteGroupBuilder CreateOrGetContainerRoutingGroup(WebApplication app)
    {
        if (_routeGroupBuilder is null)
            _routeGroupBuilder = app.MapGroup(RoutePrefix);

        //TODO: add configure for container route group builder

        return _routeGroupBuilder;
    }

    private Dictionary<string, RouteGroupBuilder> _entityRouteGroupBuider = new();
    public RouteGroupBuilder CreateOrGetEntityGroup(WebApplication app, EntityCRUDRoutingMetadata metadata)
    {
        if (!_entityRouteGroupBuider.TryGetValue(metadata.Name, out var group))
        {
            group = CreateOrGetContainerRoutingGroup(app)
                .MapGroup(metadata.Name)
                .WithTags(metadata.Name)
                .WithMetadata(metadata);
            _entityRouteGroupBuider.Add(metadata.Name, group);
        }

        return group;
    }

    public RouteGroupBuilder CreateOrGetEntityGroup(WebApplication app, EntityMetadata entityMetadata)
    {
        if (!_entityRouteGroupBuider.TryGetValue(entityMetadata.EndpointName, out var group))
        {
            group = CreateOrGetContainerRoutingGroup(app)
                .MapGroup(entityMetadata.EndpointName)
                .WithTags(entityMetadata.EndpointName);

            //TODO: add configure for entity route group builder

            _entityRouteGroupBuider.Add(entityMetadata.EndpointName, group);
        }

        return group;
    }

    public RouteGroupBuilder CreateOrGetEntityOperationGroup(WebApplication app, EntityOperationMetadata metadata)
    {
        if (!_entityRouteGroupBuider.TryGetValue(metadata.EntityRoutingName, out var group))
        {
            group = CreateOrGetContainerRoutingGroup(app)
                .MapGroup(metadata.EntityRoutingName).WithMetadata(metadata);
            _entityRouteGroupBuider.Add(metadata.EntityRoutingName, group);
        }

        return group;
    }

    private Dictionary<EntityKey, EntityCRUDRoutingMetadata> _entityMetadata = new();
    internal EntityCRUDRoutingMetadata CreateOrEditEntityMetadata(Type targetType, EntityAttribute routingAttribute)
    {
        var newMetadata = EntityCRUDRoutingMetadata.Create(targetType, routingAttribute);
        var entityKey = new EntityKey
        {
            EntityType = newMetadata.EntityType,
            KeyType = newMetadata.KeyType,
            Name = newMetadata.Name,
        };

        if (!_entityMetadata.ContainsKey(entityKey))
        {
            _entityMetadata.Add(entityKey, newMetadata);
            return newMetadata;
        }

        var configuedMetadata = _entityMetadata[entityKey];

        if (configuedMetadata.Name != newMetadata.Name)
            throw new InvalidOperationException("Entity name must be unique");

        if (configuedMetadata.KeyType != newMetadata.KeyType)
            throw new InvalidOperationException("Entity key type must be unique");

        if (configuedMetadata.EntityType != newMetadata.EntityType)
            throw new InvalidOperationException("Entity type must be unique");

        foreach (var (odataHttpMethod, serviceDescriptor) in newMetadata.ServiceDescriptors)
        {
            //Override when newMetadata is handler.
            if (newMetadata.TargetTypeIsHandler)
                configuedMetadata.ServiceDescriptors[odataHttpMethod] = serviceDescriptor;

        }

        if (newMetadata.TargetTypeIsHandler && newMetadata.AuthorizeDataList.Any())
        {
            foreach (var (method, authorizeData) in newMetadata.AuthorizeDataList)
            {
                configuedMetadata.AuthorizeDataList[method] = authorizeData;
            }
        }
        return configuedMetadata;
    }

    private Dictionary<EntityOperationKey, List<EntityOperationMetadata>> _boundOperations = new();
    internal EntityOperationMetadata CreateEntityOpration(Type targetType, BoundOperationAttribute attribute)
    {
        var entityType = attribute.EntityType;
        var entityKey = _entityMetadata.Keys.FirstOrDefault(x => x.EntityType == entityType);

        if (entityKey is null || !_entityMetadata.TryGetValue(entityKey, out var entityMetadata))
            throw new InvalidOperationException($"Entity metadata {entityType.FullName} not found");

        var newMetadata = EntityOperationMetadata.Create(entityType, entityMetadata.Name, targetType, attribute);

        var entityOperationKey = new EntityOperationKey
        {
            EntityType = entityType,
            OperationName = attribute.OperationName,
            OperationType = attribute.OperationType,
        };

        if (_boundOperations.ContainsKey(entityOperationKey))
            throw new InvalidOperationException($"Duplicate operation name: {entityOperationKey}");

        _boundOperations.Add(entityOperationKey, new List<EntityOperationMetadata> { newMetadata });
        return newMetadata;
    }


    private List<UnboundOperationMetadata> _unboundOperationMetadata = new();
    internal UnboundOperationMetadata CreateUnboundOperation(Type targetType, UnboundOperationAttribute attribute)
    {
        var newMetadata = UnboundOperationMetadata.Create(targetType, attribute);

        if (_unboundOperationMetadata.Any(x =>
            x.OperationName.Equals(newMetadata.OperationName, StringComparison.OrdinalIgnoreCase)
                && x.OperationType == newMetadata.OperationType))
        {
            throw new InvalidOperationException($"Duplicate operation name: {newMetadata}");
        }

        _unboundOperationMetadata.Add(newMetadata);

        return newMetadata;
    }

    public Dictionary<string, EntityEndpointConfiguration> DynamicEntityEndpoints = new();
    internal void CreateDynamicEntityMetadata(Type targetType, ConfigurableEntityAttribute routingAttribute)
    {
        if (DynamicEntityEndpoints.ContainsKey(routingAttribute.Name))
            throw new InvalidOperationException($"Duplicate dynamic entity name: {routingAttribute.Name}");

        if (_entityMetadata.Keys.Any(x => x.Name == routingAttribute.Name))
            throw new InvalidOperationException($"Entity name {routingAttribute.Name} already used");

        var configurationType = routingAttribute.ConfigurationType;
        if (configurationType is null)
        {
            configurationType = typeof(DefaultEfCoreConfiguration<>).MakeGenericType(targetType);
        }

        var entityEndpoint = Activator.CreateInstance(configurationType) as EntityEndpointConfiguration;
        if (entityEndpoint == null)
            throw new InvalidOperationException($"Configuration type {configurationType.FullName} must interit from EntityEndpoint");

        entityEndpoint.Name = routingAttribute.Name;
        entityEndpoint.RoutePrefix = routingAttribute.RoutePrefix;
        entityEndpoint.Methods = routingAttribute.Methods;
        entityEndpoint.BuildViewModelType(_moduleBuilder);

        DynamicEntityEndpoints.Add(routingAttribute.Name, entityEndpoint!);
    }

    internal void BuildEdmModel(EntityMimimalApiOptions coreOptions)
    {
        var modelBuilder = new ODataConventionModelBuilder();
        foreach (var (entityKey, metadata) in _entityMetadata)
        {
            var entityType = entityKey.EntityType;
            var odataEntityType = modelBuilder.AddEntityType(entityType);
            modelBuilder.AddEntitySet(metadata.Name, odataEntityType);
            var boundOperations = _boundOperations
                .Where(x => x.Key.EntityType == entityType)
                .SelectMany(x => x.Value);

            foreach (var operation in boundOperations)
            {
                if (operation.OperationType == OperationType.Action)
                {
                    var odataAction = modelBuilder.Action(operation.OperationName);

                    odataAction.SetBindingParameter(BindingParameterConfiguration.DefaultBindingParameterName, odataEntityType);
                    odataAction.Parameter(operation.RequestType, "body");

                    if (operation.ResponseType == typeof(Result))
                        continue;

                    if (operation.ResponseType.IsCommonGenericCollectionType())
                    {
                        var elementType = operation.ResponseType.GetGenericArguments().Single();
                        odataAction.ReturnsCollection(elementType);
                    }
                    else
                    {
                        odataAction.Returns(operation.ResponseType);
                    }
                    continue;
                }

                if (operation.OperationType == OperationType.Function)
                {
                    var odataFunction = modelBuilder.Function(operation.OperationName);

                    odataFunction.SetBindingParameter(BindingParameterConfiguration.DefaultBindingParameterName, odataEntityType);
                    odataFunction.Parameter(operation.RequestType, "body");

                    if (operation.ResponseType == typeof(Result))
                        throw new InvalidOperationException("Functions can't use Result type");

                    if (operation.ResponseType.IsCommonGenericCollectionType())
                    {
                        var elementType = operation.ResponseType.GetGenericArguments().Single();
                        odataFunction.ReturnsCollection(elementType);
                    }
                    else
                    {
                        odataFunction.Returns(operation.ResponseType);
                    }

                    continue;
                }
            }
        }

        var duplicationTypes = DynamicEntityEndpoints.Values
            .Select(x => x.ViewModelType)
            .GroupBy(x => x)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicationTypes.Any())
            throw new InvalidOperationException($"Duplicate view model types: {string.Join(", ", duplicationTypes)}");

        foreach (var (name, entityEndpoint) in DynamicEntityEndpoints)
        {
            var entityType = entityEndpoint.ViewModelType;
            var odataEntityType = modelBuilder.AddEntityType(entityType);
            modelBuilder.AddEntitySet(name, odataEntityType);
        }

        foreach (var operation in _unboundOperationMetadata)
        {
            if (operation.OperationType == OperationType.Action)
            {
                var odataAction = modelBuilder.Action(operation.OperationName);
                odataAction.Parameter(operation.RequestType, "body");

                if (operation.ResponseType == typeof(Result))
                    continue;

                if (operation.ResponseType.IsCommonGenericCollectionType())
                {
                    var elementType = operation.ResponseType.GetGenericArguments().Single();
                    odataAction.ReturnsCollection(elementType);
                }
                else
                {
                    odataAction.Returns(operation.ResponseType);
                }
                continue;
            }

            if (operation.OperationType == OperationType.Function)
            {
                var odataFunction = modelBuilder.Function(operation.OperationName);

                odataFunction.Parameter(operation.RequestType, "body");

                if (operation.ResponseType == typeof(Result))
                    continue;

                if (operation.ResponseType.IsCommonGenericCollectionType())
                {
                    var elementType = operation.ResponseType.GetGenericArguments().Single();
                    odataFunction.ReturnsCollection(elementType);
                }
                else
                {
                    odataFunction.Returns(operation.ResponseType);
                }
                continue;
            }
        }

        coreOptions.ConfigureModelBuilder?.Invoke(modelBuilder);

        _edmModel = modelBuilder.GetEdmModel();
        _edmModel.MarkAsImmutable();
    }

    internal void RegisterRoutingServices(IServiceCollection services)
    {
        var requestHandlerType = typeof(IHttpRequestHandler);
        var allServiceDescriptors = _entityMetadata.Values.SelectMany(x => x.ServiceDescriptors.Values)
            .Concat(_boundOperations.SelectMany(x => x.Value.Select(y => y.ServiceDescriptor)))
            .Concat(_unboundOperationMetadata.Select(x => x.ServiceDescriptor));
        var defaultMapper = new DefaultODataTypeMapper();

        foreach (var serviceDescriptor in allServiceDescriptors)
            services.Add(serviceDescriptor);

        foreach (var (entityKey, metadata) in _entityMetadata)
        {
            var entityRequestHandlerType = typeof(EntityRequestHandler<,>).MakeGenericType(entityKey.EntityType, entityKey.KeyType);
            services.AddSingleton(requestHandlerType, s
                => ActivatorUtilities.CreateInstance(s, entityRequestHandlerType, this, metadata));

            var boundOperations = _boundOperations
                .Where(x => x.Key.EntityType == entityKey.EntityType)
                .SelectMany(x => x.Value);
            foreach (var boundOperation in boundOperations)
            {
                var entityOperationRequestHandlerType = boundOperation.ResponseType == typeof(Result)
                    ? typeof(EntityOperationRequestHandler<,,>).MakeGenericType(entityKey.EntityType, entityKey.KeyType, boundOperation.RequestType)
                    : typeof(EntityOperationRequestHandler<,,,>).MakeGenericType(entityKey.EntityType, entityKey.KeyType, boundOperation.RequestType, boundOperation.ResponseType);

                services.AddSingleton(requestHandlerType, s
                    => ActivatorUtilities.CreateInstance(s, entityOperationRequestHandlerType, this, boundOperation));
            }
        }

        foreach (var (name, entityEndpoint) in DynamicEntityEndpoints)
        {
            var viewModelType = entityEndpoint.ViewModelType;
            var sourceType = entityEndpoint.SourceType;

            var entitySet = EdmModel.EntityContainer.FindEntitySet(name);
            var key = entitySet.EntityType().Key().Single();
            var keyType = defaultMapper.GetClrType(EdmModel, key.Type);
            entityEndpoint.KeyType = keyType;
            entityEndpoint.KeyPropertyName = key.Name;

            var navigationProperties = entitySet.EntityType().DeclaredNavigationProperties();
            entityEndpoint.NestedTypes = navigationProperties
                .Where(x => !x.Type.IsCollection())
                .ToDictionary(x => x.Name, x => defaultMapper.GetClrType(EdmModel, x.Type));

            entityEndpoint.CollectionTypes = navigationProperties
                .Where(x => x.Type.IsCollection())
                .ToDictionary(x => x.Name, x => defaultMapper.GetClrType(EdmModel
                , (x.Type.Definition as EdmCollectionType).ElementType));

            //Add to DI to support testing. Test project can modify the configuration
            var configurationType = typeof(EntityConfiguration<>).MakeGenericType(sourceType);
            services.AddSingleton(configurationType, entityEndpoint);

            //Purpose: hold metadata for entity
            var metadataType = typeof(EntityMetadata<,,>).MakeGenericType(sourceType, viewModelType, keyType);
            services.AddSingleton(metadataType, s => ActivatorUtilities
                .CreateInstance(s, metadataType, this, entityEndpoint));

            if (entityEndpoint.Methods.Contains(EntityMethod.Query))
            {
                //Add minimal api query request handler
                var dynamicEntityRequestHandlerType = typeof(EntityQueryRequestHandler<,,>)
                .MakeGenericType(sourceType, viewModelType, keyType);

                services.AddSingleton(requestHandlerType, s
                    => ActivatorUtilities.CreateInstance(s, dynamicEntityRequestHandlerType));
            }

            if (entityEndpoint.Methods.Contains(EntityMethod.GetByKey))
            {
                var dynamicEntityCreateRequestHandlerType = typeof(EntityGetByKeyRequestHandler<,,>)
                    .MakeGenericType(sourceType, viewModelType, keyType);
                services.AddSingleton(requestHandlerType, s
                    => ActivatorUtilities.CreateInstance(s, dynamicEntityCreateRequestHandlerType));
            }

            if (entityEndpoint.Methods.Contains(EntityMethod.Post))
            {
                var dynamicEntityCreateRequestHandlerType = typeof(EntityCreateRequestHandler<,,>)
                    .MakeGenericType(sourceType, viewModelType, keyType);
                services.AddSingleton(requestHandlerType, s
                    => ActivatorUtilities.CreateInstance(s, dynamicEntityCreateRequestHandlerType));
            }
        }

        foreach (var unboundOperation in _unboundOperationMetadata)
        {
            var isKeyed = unboundOperation.IsKeyedOperation(out var keyProp);
            var keyType = isKeyed ? keyProp!.PropertyType : typeof(object);

            var unboundOperationRequestHandlerType = unboundOperation.ResponseType == typeof(Result)
                ? typeof(UnboundOperationRequestHandler<,>).MakeGenericType(keyType, unboundOperation.RequestType)
                : typeof(UnboundOperationRequestHandler<,,>).MakeGenericType(keyType, unboundOperation.RequestType, unboundOperation.ResponseType);
            services.AddSingleton(requestHandlerType, s
                => ActivatorUtilities.CreateInstance(s, unboundOperationRequestHandlerType, this, unboundOperation));
        }
    }

}


