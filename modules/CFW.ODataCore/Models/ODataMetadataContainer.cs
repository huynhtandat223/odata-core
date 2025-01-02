using CFW.ODataCore.Attributes;
using CFW.ODataCore.ODataMetadata;
using CFW.ODataCore.RequestHandlers;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace CFW.ODataCore.Models;

public class ODataMetadataContainer
{
    public string RoutePrefix { get; }

    public ODataMetadataContainer(string routePrefix)
    {
        RoutePrefix = routePrefix;
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
            _routeGroupBuilder = app.MapGroup(RoutePrefix).WithMetadata(this);

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

    internal void BuildEdmModel()
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

        _edmModel = modelBuilder.GetEdmModel();
        _edmModel.MarkAsImmutable();
    }

    internal void RegisterRoutingServices(IServiceCollection services)
    {
        var requestHandlerType = typeof(IHttpRequestHandler);
        var allServiceDescriptors = _entityMetadata.Values.SelectMany(x => x.ServiceDescriptors.Values)
            .Concat(_boundOperations.SelectMany(x => x.Value.Select(y => y.ServiceDescriptor)))
            .Concat(_unboundOperationMetadata.Select(x => x.ServiceDescriptor));

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


