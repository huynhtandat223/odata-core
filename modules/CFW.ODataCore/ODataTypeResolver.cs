using CFW.ODataCore.Features.BoundActions;
using CFW.ODataCore.OData;
using System.Collections.Immutable;
using System.Reflection;

namespace CFW.ODataCore;

[Obsolete("Use ODataMetadataEntity instead")]
public record ODataType
{
    public required string RoutePrefix { get; set; }

    public required Type EntityType { get; set; }

    public required ODataRoutingAttribute RoutingAttribute { get; set; }

    public Type[] Interfaces => EntityType.GetInterfaces();

    public Type KeyType => Interfaces.Single(x => x.IsGenericType
        && x.GetGenericTypeDefinition() == typeof(IODataViewModel<>)).GetGenericArguments().Single();


}

public abstract class BaseODataTypeResolver
{
    protected abstract IEnumerable<Type> CachedType { get; }

    private IEnumerable<ODataType>? _odataTypes;
    private readonly string _defaultPrefix;

    public BaseODataTypeResolver(string defaultPrefix)
    {
        _defaultPrefix = defaultPrefix;
    }

    private IEnumerable<ODataType> GetTypes()
    {
        if (_odataTypes is null)
        {
            _odataTypes = CachedType
            .Where(x => x.GetCustomAttribute<ODataRoutingAttribute>() is not null)
            .Select(x => new ODataType
            {
                RoutePrefix = x.GetCustomAttribute<ODataRoutingAttribute>()!.RouteRefix ?? _defaultPrefix,
                EntityType = x,
                RoutingAttribute = x.GetCustomAttribute<ODataRoutingAttribute>()!
            }).ToImmutableArray();
        }
        return _odataTypes;
    }

    public IEnumerable<string> GetRoutePrefixes()
    {
        return GetTypes()
        .Select(x => x.RoutePrefix)
        .Distinct();
    }

    public ODataType[] GetODataTypes(string routePrefix)
    {
        return GetTypes()
        .Where(x => x.RoutePrefix == routePrefix)
        .Distinct()
        .ToArray();
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
}

public class ODataTypeResolver : BaseODataTypeResolver
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttribute<ODataRoutingAttribute>() is not null
            || x.GetCustomAttribute<BoundActionAttribute>() is not null)
        .ToList();

    public ODataTypeResolver(string defaultPrefix) : base(defaultPrefix)
    {
    }

    protected override IEnumerable<Type> CachedType => _cachedType;

}
