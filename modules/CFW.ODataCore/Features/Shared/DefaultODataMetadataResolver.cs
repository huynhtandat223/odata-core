using CFW.ODataCore.Features.BoundActions;
using CFW.ODataCore.Features.EntitySets;
using System.Reflection;

namespace CFW.ODataCore.Features.Shared;

public abstract class BaseODataMetadataResolver
{
    protected abstract IEnumerable<Type> CachedType { get; }

    private readonly string _defaultPrefix;

    public BaseODataMetadataResolver(string defaultPrefix)
    {
        _defaultPrefix = defaultPrefix;
    }

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
                    BoundActionAttribute = actionHandlerType.GetCustomAttribute<BoundActionAttribute>()!,
                    BoundActionControllerType = typeof(BoundActionsController<,,,>).MakeGenericType(
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
                    BoundActionAttribute = actionHandlerType.GetCustomAttribute<BoundActionAttribute>()!,
                    BoundActionControllerType = typeof(BoundActionsController<,,,>).MakeGenericType(
                        viewModelType, keyType, requestType, responseType).GetTypeInfo(),
                };
            }
        }
    }

    private ODataMetadataEntity CreateMetadataEntity(Type viewModelType, ODataRoutingAttribute routingAttribute
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
            SetupAttributes = viewModelType.GetCustomAttributes(),
        };
    }

    public IEnumerable<ODataMetadataContainer> CreateContainers()
    {
        var routePrefixes = CachedType
            .Where(x => x.GetCustomAttribute<ODataRoutingAttribute>() is not null)
            .Select(x => new { ViewModelType = x, RoutingAttribute = x.GetCustomAttribute<ODataRoutingAttribute>()! })
            .GroupBy(x => x.RoutingAttribute!.RouteRefix ?? _defaultPrefix);

        foreach (var group in routePrefixes)
        {
            var routePrefix = group.Key;
            var container = new ODataMetadataContainer(routePrefix);

            var oDataTypes = group
                .Select(x => CreateMetadataEntity(x.ViewModelType, x.RoutingAttribute, container));

            container.AddEntitySets(routePrefix, this, oDataTypes);
            container.Build();
            yield return container;
        }
    }
}

public class DefaultODataMetadataResolver : BaseODataMetadataResolver
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttribute<ODataRoutingAttribute>() is not null
            || x.GetCustomAttribute<BoundActionAttribute>() is not null)
        .ToList();

    public DefaultODataMetadataResolver(string defaultPrefix) : base(defaultPrefix)
    {
    }

    protected override IEnumerable<Type> CachedType => _cachedType;

}
