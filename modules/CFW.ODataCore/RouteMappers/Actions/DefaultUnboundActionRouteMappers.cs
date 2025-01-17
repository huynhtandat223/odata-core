using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Metadata;

namespace CFW.ODataCore.RouteMappers.Actions;

internal class DefaultUnboundActionRouteMapper<TRequest> : IRouteMapper
{
    private readonly MetadataUnboundAction _metadata;

    public DefaultUnboundActionRouteMapper(MetadataUnboundAction metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
        => ActionRouteMapperExtensions.MappRoutes<TRequest>(routeGroupBuilder, _metadata);
}

internal class DefaultUnboundActionRouteMapper<TRequest, TKey> : IRouteMapper
{
    private readonly MetadataUnboundAction _metadata;

    public DefaultUnboundActionRouteMapper(MetadataUnboundAction metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
        => ActionRouteMapperExtensions.MappRoutes<TRequest, TKey>(routeGroupBuilder, _metadata);
}

internal class DefaultUnboundActionHasResponseRouteMapper<TRequest, TResponse> : IRouteMapper
{
    private readonly MetadataUnboundAction _metadata;

    public DefaultUnboundActionHasResponseRouteMapper(MetadataUnboundAction metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
        => ActionRouteMapperExtensions.MappHasResponseDataRoutes<TRequest, TResponse>(routeGroupBuilder, _metadata);
}

internal class DefaultUnboundActionHasResponseRouteMapper<TRequest, TKey, TResponse> : IRouteMapper
{
    private readonly MetadataUnboundAction _metadata;

    public DefaultUnboundActionHasResponseRouteMapper(MetadataUnboundAction metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
        => ActionRouteMapperExtensions.MappHasResponseDataRoutes<TRequest, TKey, TResponse>(routeGroupBuilder
            , _metadata);
}

