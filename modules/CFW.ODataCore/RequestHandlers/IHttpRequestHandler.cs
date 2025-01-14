namespace CFW.ODataCore.RequestHandlers;

public interface IHttpRequestHandler
{
    Task MappRouters(WebApplication app);
}

public interface IRouteMapper
{
    Task MappRoutes(RouteGroupBuilder routeGroupBuilder, WebApplication app);
}


