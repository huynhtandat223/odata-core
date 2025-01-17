using CFW.Core.Builders;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Metadata;
using CFW.ODataCore.RouteMappers;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RouteMappers.Actions;

public static class ActionRouteMapperExtensions
{
    public static string[] MapHttpMethods(ApiMethod apiMethod)
    {
        return apiMethod switch
        {
            ApiMethod.Get => ["GET"],
            ApiMethod.Post => ["POST"],
            ApiMethod.Put => ["PUT"],
            ApiMethod.Patch => ["PATCH"],
            ApiMethod.Delete => ["DELETE"],
            _ => throw new InvalidOperationException("Invalid Method")
        };
    }

    private static Dictionary<Type, Delegate> _setters = new();

    public static Task MappRoutes<TRequest>(RouteGroupBuilder routeGroupBuilder, MetadataAction actionMetadata)
    {
        var mappedMethods = MapHttpMethods(actionMetadata.HttpMethod);
        var actionName = actionMetadata.ActionName;

        routeGroupBuilder.MapMethods(actionName, mappedMethods, async (
                [FromServices] IOperationHandler<TRequest> handler
                , [FromBody] TRequest? request, QueryRequest<TRequest> queryRequest
                , HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            request ??= queryRequest.QueryModel;
            if (request is null)
                return Results.BadRequest("Invalid Request");

            var result = await handler.Handle(request, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }

    public static Task MappRoutes<TRequest, TKey>(RouteGroupBuilder routeGroupBuilder, MetadataAction actionMetadata)
    {
        var mappedMethods = MapHttpMethods(actionMetadata.HttpMethod);
        var keyProperty = actionMetadata.KeyProperty!;
        var actionName = actionMetadata.ActionName;

        var routePattern = actionMetadata is MetadataEntityAction
                ? $"{{key}}/{actionName}"
                : $"{actionName}/{{key}}";

        routeGroupBuilder.MapMethods(routePattern, mappedMethods, async (
            [FromServices] IOperationHandler<TRequest> handler
            , [FromBody] TRequest? request, QueryRequest<TRequest> queryRequest
            , TKey key
            , HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            request ??= queryRequest.QueryModel;
            if (request is null)
                return Results.BadRequest("Invalid Request");

            //set key to request
            if (!_setters.TryGetValue(keyProperty.PropertyType, out var setter))
            {
                var expr = keyProperty!.BuildSetter();
                setter = expr.Compile();
                _setters[keyProperty.PropertyType] = setter;
            }
            setter.DynamicInvoke(request, key);


            var result = await handler.Handle(request, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }

    public static Task MappHasResponseDataRoutes<TRequest, TResponse>(RouteGroupBuilder routeGroupBuilder
        , MetadataAction actionMetadata)
    {
        var mappedMethods = MapHttpMethods(actionMetadata.HttpMethod);
        var actionName = actionMetadata.ActionName;

        routeGroupBuilder.MapMethods(actionName, mappedMethods, async (
                [FromServices] IOperationHandler<TRequest, TResponse> handler
                , [FromBody] TRequest? request, QueryRequest<TRequest> queryRequest
                , CancellationToken cancellationToken) =>
        {
            request ??= queryRequest.QueryModel;
            if (request is null)
                return Results.BadRequest("Invalid Request");

            var result = await handler.Handle(request, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }

    public static Task MappHasResponseDataRoutes<TRequest, TKey, TResponse>(RouteGroupBuilder routeGroupBuilder
        , MetadataAction actionMetadata)
    {
        var mappedMethods = MapHttpMethods(actionMetadata.HttpMethod);
        var keyProperty = actionMetadata.KeyProperty!;
        var actionName = actionMetadata.ActionName;

        var routePattern = actionMetadata is MetadataEntityAction
                ? $"{{key}}/{actionName}"
                : $"{actionName}/{{key}}";

        routeGroupBuilder.MapMethods(routePattern, mappedMethods, async (
          [FromServices] IOperationHandler<TRequest, TResponse> handler
          , [FromBody] TRequest? request, QueryRequest<TRequest> queryRequest
            , TKey key,
        HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            request ??= queryRequest.QueryModel;
            if (request is null)
                return Results.BadRequest("Invalid Request");

            //set key to request
            if (!_setters.TryGetValue(keyProperty.PropertyType, out var setter))
            {
                var expr = keyProperty!.BuildSetter();
                setter = expr.Compile();
                _setters[keyProperty.PropertyType] = setter;
            }

            setter.DynamicInvoke(request, key);

            var result = await handler.Handle(request, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }
}
