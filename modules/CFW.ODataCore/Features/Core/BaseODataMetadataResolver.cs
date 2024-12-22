using CFW.ODataCore.Features.BoundOperations;
using CFW.ODataCore.Features.EntitySets;
using CFW.ODataCore.Features.Shared;
using CFW.ODataCore.Features.UnBoundActions;
using CFW.ODataCore.Features.UnboundFunctions;
using System.Reflection;

namespace CFW.ODataCore.Features.Core;

public abstract class BaseODataMetadataResolver
{
    protected abstract IEnumerable<Type> CachedType { get; }

    private readonly string _defaultPrefix;

    public BaseODataMetadataResolver(string defaultPrefix)
    {
        _defaultPrefix = defaultPrefix;
    }

    internal IEnumerable<ODataBoundOperationMetadata> GetBoundOperationMetadataList(Type viewModelType, Type keyType
        , ODataMetadataContainer container
        , string boundCollectionName)
    {
        var boundOperationAttr = typeof(BoundOperationAttribute);
        var withResponseHandlerInterfaceType = typeof(IODataActionHandler<,>);
        var nonResponseInterfaceType = typeof(IODataActionHandler<>);

        var handlerTypeInfos = CachedType
            .Where(x => x.GetCustomAttribute(boundOperationAttr) is not null)
            .Select(x => new { HandlerType = x, Attribute = x.GetCustomAttribute<BoundOperationAttribute>()! })
            .Where(x => x.Attribute.KeyType == keyType && x.Attribute.ViewModelType == viewModelType)
            .ToArray();

        if (handlerTypeInfos.Length == 0)
            yield break;

        //Check viewModelType routing name is same as boundCollectionName
        var hasDiffEntitySetRouting = handlerTypeInfos
            .Any(x => x.Attribute.ViewModelType.GetCustomAttribute<ODataEntitySetAttribute>() is null
                || x.Attribute.ViewModelType.GetCustomAttribute<ODataEntitySetAttribute>()!.Name != boundCollectionName);
        if (hasDiffEntitySetRouting)
            throw new InvalidOperationException($"Bound operation handler for {viewModelType} has different routing name than the entity set name");

        foreach (var handlerTypeInfo in handlerTypeInfos)
        {
            var handlerType = handlerTypeInfo.HandlerType;
            var routingAttribute = handlerTypeInfo.Attribute;
            var interfaces = handlerType.GetInterfaces();
            var withResponseHandlerInterface = interfaces
                .SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == withResponseHandlerInterfaceType);

            if (withResponseHandlerInterface is not null)
            {
                var requestType = withResponseHandlerInterface.GetGenericArguments().First();
                var responseType = withResponseHandlerInterface.GetGenericArguments().Last();

                yield return new ODataBoundOperationMetadata
                {
                    OperationType = routingAttribute.OperationType,
                    KeyType = keyType,
                    SetupAttributes = handlerType.GetCustomAttributes(),
                    BoundCollectionName = boundCollectionName,
                    Container = container,
                    RequestType = requestType,
                    ResponseType = responseType,
                    HandlerType = handlerType,
                    BoundOprationAttribute = routingAttribute,
                    ControllerType = typeof(BoundOperationsController<,,,>).MakeGenericType(
                        viewModelType, keyType, requestType, responseType).GetTypeInfo(),
                };
            }

            var nonResponseHandlerInterface = interfaces
                .SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == nonResponseInterfaceType);
            if (nonResponseHandlerInterface is not null)
            {
                var requestType = nonResponseHandlerInterface.GetGenericArguments().Single();
                var responseType = typeof(Result);

                yield return new ODataBoundOperationMetadata
                {
                    OperationType = routingAttribute.OperationType,
                    KeyType = keyType,
                    SetupAttributes = handlerType.GetCustomAttributes(),
                    BoundCollectionName = boundCollectionName,
                    Container = container,
                    RequestType = requestType,
                    ResponseType = responseType,
                    HandlerType = handlerType,
                    BoundOprationAttribute = handlerType.GetCustomAttribute<BoundOperationAttribute>()!,
                    ControllerType = typeof(BoundOperationsController<,,,>).MakeGenericType(
                        viewModelType, keyType, requestType, responseType).GetTypeInfo(),
                };
            }
        }
    }

    private ODataMetadataEntity CreateMetadataEntity(Type viewModelType, ODataEntitySetAttribute routingAttribute
        , ODataMetadataContainer container)
    {
        var odataViewModelInterface = viewModelType.GetInterfaces()
            .Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IODataViewModel<>));
        var keyType = odataViewModelInterface.GetGenericArguments().Single();

        return new ODataMetadataEntity
        {
            DataRoutingAttribute = routingAttribute,
            Container = container,
            ViewModelType = viewModelType,
            ControllerType = typeof(EntitySetsController<,>).MakeGenericType(viewModelType, keyType).GetTypeInfo(),
            Name = routingAttribute.Name,
            BoundOperationMetadataList = GetBoundOperationMetadataList(viewModelType, keyType, container, routingAttribute.Name),
            SetupAttributes = viewModelType.GetCustomAttributes(),
        };
    }

    private UnboundActionMetadata CreateUnboundActionMetadata(Type handlerType, UnboundActionAttribute unboundActionAttribute)
    {
        var actionHandlerInterface = handlerType
            .GetInterfaces()
            .SingleOrDefault(x => x.IsGenericType
                && x.GetGenericTypeDefinition() == typeof(IODataActionHandler<>));

        if (actionHandlerInterface is null)
        {
            actionHandlerInterface = handlerType
                .GetInterfaces()
                .SingleOrDefault(x => x.IsGenericType
                    && x.GetGenericTypeDefinition() == typeof(IODataActionHandler<,>));
        }

        if (actionHandlerInterface is null)
            throw new InvalidOperationException($"Handler type {handlerType} does not implement IODataActionHandler<> or IODataActionHandler<,>");

        var args = actionHandlerInterface.GetGenericArguments();
        var requestType = args[0];
        var responseType = args.Count() == 1 ? typeof(Result) : args[1];

        return new UnboundActionMetadata
        {
            HandlerType = handlerType,
            RequestType = requestType,
            ResponseType = responseType,
            Attribute = unboundActionAttribute,
            SetupAttributes = handlerType.GetCustomAttributes(),
            ControllerType = typeof(UnboundActionsController<,>).MakeGenericType(requestType, responseType).GetTypeInfo(),
        };
    }

    private UnboundFunctionMetadata CreateUnboundFunctionMetadata(Type handlerType, UnboundFunctionAttribute attribute)
    {
        var actionHandlerInterface = handlerType
                .GetInterfaces()
                .SingleOrDefault(x => x.IsGenericType
                    && x.GetGenericTypeDefinition() == typeof(IODataActionHandler<,>));

        if (actionHandlerInterface is null)
            throw new InvalidOperationException($"Handler type {handlerType} does not implement IODataActionHandler<,>");

        var args = actionHandlerInterface.GetGenericArguments();
        var requestType = args[0];
        var responseType = args[1];

        return new UnboundFunctionMetadata
        {
            HandlerType = handlerType,
            RequestType = requestType,
            ResponseType = responseType,
            RoutingAttribute = attribute,
            SetupAttributes = handlerType.GetCustomAttributes(),
            ControllerType = typeof(UnboundFunctionsController<,>).MakeGenericType(requestType, responseType).GetTypeInfo(),
        };
    }

    public IEnumerable<ODataMetadataContainer> CreateContainers()
    {
        var containers = new List<ODataMetadataContainer>();
        var routePrefixes = CachedType
            .Where(x => x.GetCustomAttribute<ODataEntitySetAttribute>() is not null)
            .Select(x => new { ViewModelType = x, RoutingAttribute = x.GetCustomAttribute<ODataEntitySetAttribute>()! })
            .GroupBy(x => x.RoutingAttribute!.RouteRefix ?? _defaultPrefix);

        foreach (var group in routePrefixes)
        {
            var routePrefix = group.Key;
            var container = new ODataMetadataContainer(routePrefix);

            var oDataTypes = group
                .Select(x => CreateMetadataEntity(x.ViewModelType, x.RoutingAttribute, container));

            container.AddEntitySets(routePrefix, this, oDataTypes);
            containers.Add(container);
        }

        var unboudActionMetadataGroup = CachedType
            .Where(x => x.GetCustomAttribute<UnboundActionAttribute>() is not null)
            .Select(x => new
            {
                HandlerType = x,
                UnboundActionAttribute = x.GetCustomAttribute<UnboundActionAttribute>()!,
            })
            .GroupBy(x => x.UnboundActionAttribute!.RouteRefix ?? _defaultPrefix);
        foreach (var group in unboudActionMetadataGroup)
        {
            var routePrefix = group.Key;
            var container = containers.FirstOrDefault(x => x.RoutePrefix == routePrefix);
            if (container is null)
            {
                container = new ODataMetadataContainer(routePrefix);
                containers.Add(container);
            }

            var unboundActionMetadataList = group
                .Select(x => CreateUnboundActionMetadata(x.HandlerType, x.UnboundActionAttribute))
                .ToList();

            container.AddUnboundActions(unboundActionMetadataList);
        }

        var unboundFunctionMetadataGroup = CachedType
            .Where(x => x.GetCustomAttribute<UnboundFunctionAttribute>() is not null)
            .Select(x => new
            {
                HandlerType = x,
                UnboundFunctionAttribute = x.GetCustomAttribute<UnboundFunctionAttribute>()!,
            })
            .GroupBy(x => x.UnboundFunctionAttribute!.RoutePrefix ?? _defaultPrefix);
        foreach (var group in unboundFunctionMetadataGroup)
        {
            var routePrefix = group.Key;
            var container = containers.FirstOrDefault(x => x.RoutePrefix == routePrefix);
            if (container is null)
            {
                container = new ODataMetadataContainer(routePrefix);
                containers.Add(container);
            }

            var unboundFunctionMetadataList = group
                .Select(x => CreateUnboundFunctionMetadata(x.HandlerType, x.UnboundFunctionAttribute))
                .ToList();
            container.AddUnboundFunctions(unboundFunctionMetadataList);
        }

        containers.ForEach(x => x.Build());
        return containers;
    }
}
