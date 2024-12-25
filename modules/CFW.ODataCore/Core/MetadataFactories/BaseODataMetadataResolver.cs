using CFW.ODataCore.Core.Attributes;
using CFW.ODataCore.Core.Metadata;
using CFW.ODataCore.Features.BoundOperations;
using CFW.ODataCore.Features.EntityCreate;
using CFW.ODataCore.Features.EntityOperations;
using CFW.ODataCore.Features.EntityQuery;
using CFW.ODataCore.Features.UnBoundOperations;
using System.Reflection;

namespace CFW.ODataCore.Core.MetadataResolvers;

public abstract class BaseODataMetadataResolver
{
    protected abstract IEnumerable<Type> CachedType { get; }

    private readonly string _defaultPrefix;

    public BaseODataMetadataResolver(string defaultPrefix)
    {
        _defaultPrefix = defaultPrefix;
    }

    public IEnumerable<ODataMetadataContainer> CreateContainers()
    {
        var containers = new List<ODataMetadataContainer>();
        var routePrefixes = CachedType
            .SelectMany(x => x.GetCustomAttributes<EndpointEntityActionAttribute>().Select(c => new { ApplyType = x, RoutingAttribute = c }))
            .GroupBy(x => x.RoutingAttribute!.RoutePrefix ?? _defaultPrefix);

        //Register EntitySets
        foreach (var group in routePrefixes)
        {
            var routePrefix = group.Key;
            var container = new ODataMetadataContainer(routePrefix);
            containers.Add(container);

            var newEntitySetMetadataList = group
                .Select(x => CreateEntitySetMetadata(x.ApplyType, x.RoutingAttribute, container))
                .ToList();

            container.EntitySetMetadataList.AddRange(newEntitySetMetadataList);
        }

        //register bound operations
        var boundOperationByGroup = CachedType
            .Where(x => x.GetCustomAttributes<EntityOperationAttribute>().Any())
            .SelectMany(x => x.GetCustomAttributes<EntityOperationAttribute>()
                .Select(attr => new { HandlerType = x, RoutingAttribute = attr }))
            .GroupBy(x => x.RoutingAttribute!.RoutePrefix ?? _defaultPrefix);

        foreach (var group in boundOperationByGroup)
        {
            var routePrefix = group.Key;
            var container = containers.FirstOrDefault(x => x.RoutePrefix == routePrefix);
            if (container is null)
            {
                container = new ODataMetadataContainer(routePrefix);
                containers.Add(container);
            }

            var entitySetGroup = group.GroupBy(x => new { x.RoutingAttribute.BoundEntityType, x.RoutingAttribute.BoundKeyType });
            foreach (var entitySetInfo in entitySetGroup)
            {
                var entitySetMetadata = container.EntitySetMetadataList
                    .SingleOrDefault(x => x.Container.RoutePrefix == routePrefix && x.ViewModelType == entitySetInfo.Key.BoundEntityType
                        && x.KeyType == entitySetInfo.Key.BoundKeyType);

                if (entitySetMetadata is null)
                    throw new InvalidOperationException($"EntitySetMetadata not found for {entitySetInfo.Key.BoundEntityType} " +
                        $"and {entitySetInfo.Key.BoundKeyType}");

                var boundOperationMetadataListOfEntitySet = entitySetInfo
                    .Select(x => CreateBoundOperationMetadata(entitySetMetadata, x.HandlerType, x.RoutingAttribute));

                container.BoundOperationMetadataList.AddRange(boundOperationMetadataListOfEntitySet);
            }
        }

        //Register unbound operations
        var unboudOperationMetadataGroup = CachedType
            .Where(x => x.GetCustomAttributes<UnboundOperationAttribute>().Any())
            .SelectMany(x => x.GetCustomAttributes<UnboundOperationAttribute>()
                .Select(attr => new { HandlerType = x, RoutingAttribute = attr }))
            .GroupBy(x => x.RoutingAttribute!.RoutePrefix ?? _defaultPrefix);
        foreach (var group in unboudOperationMetadataGroup)
        {
            var routePrefix = group.Key;
            var container = containers.FirstOrDefault(x => x.RoutePrefix == routePrefix);
            if (container is null)
            {
                container = new ODataMetadataContainer(routePrefix);
                containers.Add(container);
            }

            var unboundOperationMetadataList = group
                .Select(x => CreateUnboundMetadata(container, x.HandlerType, x.RoutingAttribute))
                .ToList();
            container.UnBoundOperationMetadataList.AddRange(unboundOperationMetadataList);
        }

        containers.ForEach(container => container.Build());

        return containers;
    }

    private EntitySetMetadata CreateEntitySetMetadata(Type applyType, EndpointEntityActionAttribute routingAttribute
        , ODataMetadataContainer container)
    {
        var viewModelType = routingAttribute.BoundEntityType;
        var keyType = routingAttribute.BoundKeyType;

        if (viewModelType is null)
        {
            var odataViewModelInterface = applyType.GetInterfaces()
                .Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IODataViewModel<>));

            viewModelType = applyType;
            keyType = odataViewModelInterface.GetGenericArguments().Single();
        }

        var entitySetMethodMapping = new Dictionary<EndpointAction, (string ActionName, Type ControllerType
            , Type ServiceHandlerType, Type ServiceImplemenationType)>
        {
            [EndpointAction.PostCreate] = (nameof(EntityCreateController<RefODataViewModel, int>.Post)
                , typeof(EntityCreateController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityCreateHandler<,>), typeof(EntityCreateDefaultHandler<,>)),

            [EndpointAction.Query] = (nameof(EntityQueryController<RefODataViewModel, int>.Query)
                , typeof(EntityQueryController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityQueryHandler<,>), typeof(EntityQueryDefaultHandler<,>)),

            [EndpointAction.GetByKey] = (nameof(EntityGetByKeyController<RefODataViewModel, int>.GetByKey)
                , typeof(EntityGetByKeyController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityGetByKeyHandler<,>), typeof(EntityGetByKeyDefaultHandler<,>)),

            [EndpointAction.PatchUpdate] = (nameof(EntityPatchController<RefODataViewModel, int>.Patch)
                , typeof(EntityPatchController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityGetByKeyHandler<,>), typeof(EntityGetByKeyDefaultHandler<,>)),

            [EndpointAction.Delete] = (nameof(EntityDeleteController<RefODataViewModel, int>.Delete)
                , typeof(EntityDeleteController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityDeleteHandler<,>), typeof(EntityDeleteDefaultHandler<,>)),
        };

        if (entitySetMethodMapping.TryGetValue(routingAttribute.EndpointAction, out var mappingInfo))
        {
            var metadata = new EntitySetMetadata
            {
                ServiceHandlerType = mappingInfo.ServiceHandlerType,
                ServiceImplemenationType = mappingInfo.ServiceImplemenationType,
                DbSetType = routingAttribute.DbSetType,
                RoutingAttribute = routingAttribute,
                Container = container,
                ViewModelType = viewModelType,
                KeyType = keyType,
                ControllerActionMethodName = mappingInfo.ActionName,
                ControllerType = mappingInfo.ControllerType.GetTypeInfo(),
                SetupAttributes = applyType.GetCustomAttributes(),
            };
            return metadata;
        }

        throw new InvalidOperationException($"Invalid ODataMethod {routingAttribute.EndpointAction} for {applyType}");
    }

    public BoundOperationMetadata CreateBoundOperationMetadata(EntitySetMetadata entitySetMetadata
        , Type operationImplementationType
        , EntityOperationAttribute operationRoutingAttribute)
    {
        var operationServiceTypes = new Type[] { typeof(IEntityOperationHandler<,>), typeof(IEntityOperationHandler<>) };
        var implementationIntefaces = operationImplementationType.GetInterfaces();
        var operationInteface = implementationIntefaces
            .Single(x => x.IsGenericType && operationServiceTypes.Contains(x.GetGenericTypeDefinition()));

        var args = operationInteface.GetGenericArguments();
        var isNonResponseHandler = args.Length == 1;
        var requestType = args.First();
        var responseType = isNonResponseHandler ? typeof(Result) : args[1];

        var controllerType = typeof(EntityBoundOprationsController<,,,>).MakeGenericType(
            operationRoutingAttribute.BoundEntityType, operationRoutingAttribute.BoundKeyType, requestType, responseType).GetTypeInfo();

        var controllerActionMethodName = operationRoutingAttribute.EndpointAction switch
        {
            EndpointAction.BoundAction => isNonResponseHandler
                ? nameof(EntityBoundOprationsController<RefODataViewModel, int, object, object>.ExecuteNonResponseBoundAction)
                : nameof(EntityBoundOprationsController<RefODataViewModel, int, object, object>.ExecuteBoundAction),
            EndpointAction.BoundFunction => nameof(EntityBoundOprationsController<RefODataViewModel, int, object, object>.ExecuteBoundFunction),
            _ => throw new NotImplementedException(),
        };

        return new BoundOperationMetadata
        {
            BoundEntitySetMetadata = entitySetMetadata,
            Container = entitySetMetadata.Container,
            RequestType = requestType,
            ResponseType = responseType,
            ControllerActionMethodName = controllerActionMethodName,
            ControllerType = controllerType,
            RoutingAttribute = operationRoutingAttribute,
            SetupAttributes = operationImplementationType.GetCustomAttributes(),
            ServiceHandlerType = operationInteface,
            ServiceImplemenationType = operationImplementationType,
        };
    }

    private UnboundOperationMetadata CreateUnboundMetadata(ODataMetadataContainer container, Type handlerType
        , UnboundOperationAttribute attribute)
    {
        var implementationInterfaces = handlerType.GetInterfaces();
        var unboundOperationInterfaces = new Type[] { typeof(IUnboundOperationHandler<>), typeof(IUnboundOperationHandler<,>) };

        var handlerInterface = handlerType
            .GetInterfaces()
            .SingleOrDefault(x => x.IsGenericType && unboundOperationInterfaces.Contains(x.GetGenericTypeDefinition()));

        if (handlerInterface is null)
            throw new InvalidOperationException($"Handler type {handlerType} does not implement IODataActionHandler<> or IODataActionHandler<,>");

        var args = handlerInterface.GetGenericArguments();
        var nonResponseHandler = args.Length == 1;
        var requestType = args[0];
        var responseType = nonResponseHandler ? typeof(Result) : args[1];
        var controlerActionName = attribute.EndpointAction switch
        {
            EndpointAction.UnboundAction => nonResponseHandler
                ? nameof(UnboundOprationsController<object, object>.ExecuteNoResponseAction)
                : nameof(UnboundOprationsController<object, object>.ExecuteUnboundAction),
            EndpointAction.UnboundFunction => nameof(UnboundOprationsController<object, object>.ExecuteUnBoundFunction),
            _ => throw new NotImplementedException(),
        };

        return new UnboundOperationMetadata
        {
            ServiceHandlerType = handlerInterface,
            ControllerActionMethodName = controlerActionName,
            Container = container,
            ServiceImplemenationType = handlerType,
            RequestType = requestType,
            ResponseType = responseType,
            RoutingAttribute = attribute,
            SetupAttributes = handlerType.GetCustomAttributes(),
            ControllerType = typeof(UnboundOprationsController<,>).MakeGenericType(requestType, responseType).GetTypeInfo(),
        };
    }
}
