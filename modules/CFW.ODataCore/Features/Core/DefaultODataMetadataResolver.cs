using CFW.ODataCore.Features.BoundActions;
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

    [Obsolete]
    internal IEnumerable<ODataBoundActionMetadata> GetBoundActionMetadataList(Type viewModelType, Type keyType
        , ODataMetadataContainer container
        , string boundCollectionName)
    {
        var boundActionAttrType = typeof(BoundActionAttribute<,>).MakeGenericType(viewModelType, keyType);
        var actionWithResponseHandlerInterfaceType = typeof(IODataActionHandler<,>);
        var actionHandlerInterfaceType = typeof(IODataActionHandler<>);

        var actionHandlerTypes = CachedType
            .Where(x => x.GetCustomAttribute(boundActionAttrType) is not null)
            .ToArray();

        foreach (var actionHandlerType in actionHandlerTypes)
        {
            var interfaces = actionHandlerType.GetInterfaces();
            var actionWithResponseHandlerInterface = interfaces
                .SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == actionWithResponseHandlerInterfaceType);

            if (actionWithResponseHandlerInterface is not null)
            {
                var requestType = actionWithResponseHandlerInterface.GetGenericArguments().First();
                var responseType = actionWithResponseHandlerInterface.GetGenericArguments().Last();

                yield return new ODataBoundActionMetadata
                {
                    KeyType = keyType,
                    SetupAttributes = actionHandlerType.GetCustomAttributes().ToArray(),
                    BoundCollectionName = boundCollectionName,
                    Container = container,
                    RequestType = requestType,
                    ResponseType = responseType,
                    HandlerType = actionHandlerType,
                    BoundActionAttribute = actionHandlerType.GetCustomAttribute<BoundOperationAttribute>()!,
                    ControllerType = typeof(BoundActionsController<,,,>).MakeGenericType(
                        viewModelType, keyType, requestType, responseType).GetTypeInfo(),
                };
            }

            var actionHandlerInterface = interfaces
                .SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == actionHandlerInterfaceType);
            if (actionHandlerInterface is not null)
            {
                var requestType = actionHandlerInterface.GetGenericArguments().Single();
                var responseType = typeof(Result);

                yield return new ODataBoundActionMetadata
                {
                    KeyType = keyType,
                    SetupAttributes = actionHandlerType.GetCustomAttributes().ToArray(),
                    BoundCollectionName = boundCollectionName,
                    Container = container,
                    RequestType = requestType,
                    ResponseType = responseType,
                    HandlerType = actionHandlerType,
                    BoundActionAttribute = actionHandlerType.GetCustomAttribute<BoundOperationAttribute>()!,
                    ControllerType = typeof(BoundActionsController<,,,>).MakeGenericType(
                        viewModelType, keyType, requestType, responseType).GetTypeInfo(),
                };
            }
        }
    }

    internal IEnumerable<ODataBoundActionMetadata> GetBoundOperationMetadataList(Type viewModelType, Type keyType
        , ODataMetadataContainer container
        , string boundCollectionName)
    {
        var boundRoutingAttrType = typeof(BoundFunctionAttribute<,>).MakeGenericType(viewModelType, keyType);
        var withResponseOprHandler = typeof(IODataActionHandler<,>);

        var handlerTypes = CachedType
            .Where(x => x.GetCustomAttribute(boundRoutingAttrType) is not null)
            .ToArray();

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces();
            var actionWithResponseHandlerInterface = interfaces
                .SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == withResponseOprHandler);

            if (actionWithResponseHandlerInterface is not null)
            {
                var requestType = actionWithResponseHandlerInterface.GetGenericArguments().First();
                var responseType = actionWithResponseHandlerInterface.GetGenericArguments().Last();

                yield return new ODataBoundActionMetadata
                {
                    KeyType = keyType,
                    SetupAttributes = handlerType.GetCustomAttributes().ToArray(),
                    BoundCollectionName = boundCollectionName,
                    Container = container,
                    RequestType = requestType,
                    ResponseType = responseType,
                    HandlerType = handlerType,
                    BoundActionAttribute = handlerType.GetCustomAttribute<BoundOperationAttribute>()!,
                    ControllerType = typeof(BoundOperationsController<,,,>).MakeGenericType(
                        viewModelType, keyType, requestType, responseType).GetTypeInfo(),
                };
            }

            //var actionHandlerInterface = interfaces
            //    .SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == actionHandlerInterfaceType);
            //if (actionHandlerInterface is not null)
            //{
            //    var requestType = actionHandlerInterface.GetGenericArguments().Single();
            //    var responseType = typeof(Result);

            //    yield return new ODataBoundActionMetadata
            //    {
            //        KeyType = keyType,
            //        SetupAttributes = actionHandlerType.GetCustomAttributes().ToArray(),
            //        BoundCollectionName = boundCollectionName,
            //        Container = container,
            //        RequestType = requestType,
            //        ResponseType = responseType,
            //        HandlerType = actionHandlerType,
            //        BoundActionAttribute = actionHandlerType.GetCustomAttribute<BoundActionAttribute>()!,
            //        ControllerType = typeof(BoundActionsController<,,,>).MakeGenericType(
            //            viewModelType, keyType, requestType, responseType).GetTypeInfo(),
            //    };
            //}
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
            BoundActionMetadataList = GetBoundActionMetadataList(viewModelType, keyType, container, routingAttribute.Name).ToList(),
            BoundFunctionMetadataList = GetBoundOperationMetadataList(viewModelType, keyType, container, routingAttribute.Name).ToList(),
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

public class DefaultODataMetadataResolver : BaseODataMetadataResolver
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttribute<ODataEntitySetAttribute>() is not null
            || x.GetCustomAttribute<BoundOperationAttribute>() is not null
            || x.GetCustomAttribute<UnboundActionAttribute>() is not null
            || x.GetCustomAttribute<UnboundFunctionAttribute>() is not null)
        .ToList();

    public DefaultODataMetadataResolver(string defaultPrefix) : base(defaultPrefix)
    {
    }

    protected override IEnumerable<Type> CachedType => _cachedType;

}
