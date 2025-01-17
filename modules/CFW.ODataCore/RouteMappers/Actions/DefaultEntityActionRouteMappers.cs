using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Metadata;

namespace CFW.ODataCore.RouteMappers.Actions;

internal class DefaultEntityActionRequestHandler<TRequest> : IRouteMapper
{
    private readonly MetadataAction _metadata;

    public DefaultEntityActionRequestHandler(MetadataAction metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
        => ActionRouteMapperExtensions.MappRoutes<TRequest>(routeGroupBuilder, _metadata);
}

internal class DefaultEntityActionRequestHandler<TRequest, TKey> : IRouteMapper
{
    private readonly MetadataAction _metadata;

    public DefaultEntityActionRequestHandler(MetadataAction metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
        => ActionRouteMapperExtensions.MappRoutes<TRequest, TKey>(routeGroupBuilder, _metadata);
}

internal class DefaultEntityActionHasResponseRequestHandler<TRequest, TResponse> : IRouteMapper
{
    private readonly MetadataAction _metadata;

    public DefaultEntityActionHasResponseRequestHandler(MetadataAction metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
        => ActionRouteMapperExtensions.MappHasResponseDataRoutes<TRequest, TResponse>(routeGroupBuilder, _metadata);
}

internal class DefaultEntityActionHasResponseRequestHandler<TRequest, TKey, TResponse> : IRouteMapper
{
    private readonly MetadataAction _metadata;

    public DefaultEntityActionHasResponseRequestHandler(MetadataAction metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
        => ActionRouteMapperExtensions.MappHasResponseDataRoutes<TRequest, TKey, TResponse>(routeGroupBuilder, _metadata);
}

