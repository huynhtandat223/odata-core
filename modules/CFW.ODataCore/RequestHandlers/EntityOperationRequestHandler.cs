using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.ODataMetadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Web;

namespace CFW.ODataCore.RequestHandlers;

public class EntityActionRequestContext : EntityRequestContext
{
    public required MetadataEntityAction EntityActionMetadata { get; init; }
}

public interface IEntityActionRequestHandler
{
    Task MappRoutes(EntityActionRequestContext entityRequestContext);
}

public interface IEntityActionRequestHandler<TRequest, TResponse> : IEntityActionRequestHandler
{
}

public interface IEntityActionRequestHandler<TRequest> : IEntityActionRequestHandler
{
}

public class QueryRequest<TRequest>
{
    public TRequest? Request { get; set; } = default!;

    public static ValueTask<QueryRequest<TRequest>> BindAsync(HttpContext context)
    {
        var request = context.Request;
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

        if (!request.QueryString.HasValue)
        {
            return default;
        }

        var dict = HttpUtility.ParseQueryString(request.QueryString.Value);
        string json = JsonSerializer.Serialize(dict.Cast<string>().ToDictionary(k => k, v => dict[v]));
        var model = JsonSerializer.Deserialize<TRequest>(json, jsonOptions.JsonSerializerOptions);

        var result = new QueryRequest<TRequest>
        {
            Request = model
        };
        return new ValueTask<QueryRequest<TRequest>>(result);
    }

}

public abstract class DefaultEntityActionRequestHandler
{
    private string[] MapHttpMethods(ApiMethod apiMethod)
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

    public Task MappRoutes<TRequest>(EntityActionRequestContext entityActionRequestContext)
    {
        var entityGroup = entityActionRequestContext.EntityRouteGroupBuider;
        var actionName = entityActionRequestContext.EntityActionMetadata.ActionName;
        var mappedMethods = MapHttpMethods(entityActionRequestContext.EntityActionMetadata.HttpMethod);
        var keyProperty = entityActionRequestContext.EntityActionMetadata.KeyProperty;

        if (keyProperty is not null)
        {
            entityGroup.MapMethods($"{{key}}/{actionName}", mappedMethods, async (
                [FromBody] TRequest request, QueryRequest<TRequest> queryRequest
                , string key
                , HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var keyValue = key.Parse(keyProperty.PropertyType);
                keyProperty.SetValue(request, keyValue);

                var serviceProvider = httpContext.RequestServices;
                var handlerObj = ActivatorUtilities.CreateInstance(serviceProvider
                    , entityActionRequestContext.EntityActionMetadata.TargetType);

                if (handlerObj is not IOperationHandler<TRequest> handler)
                    throw new InvalidOperationException("Invalid Handler");

                if (handler is null)
                    throw new InvalidOperationException("Invalid Handler");

                var result = await handler.Handle(request, cancellationToken);
                return result.ToResults();
            });
        }
        else
        {
            entityGroup.MapMethods(actionName, mappedMethods, async (
                [FromBody] TRequest? request, QueryRequest<TRequest> queryRequest
                , HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                request ??= queryRequest.Request!;

                var serviceProvider = httpContext.RequestServices;
                var handlerObj = ActivatorUtilities.CreateInstance(serviceProvider
                    , entityActionRequestContext.EntityActionMetadata.TargetType);

                if (handlerObj is not IOperationHandler<TRequest> handler)
                    throw new InvalidOperationException("Invalid Handler");

                if (handler is null)
                    throw new InvalidOperationException("Invalid Handler");

                var result = await handler.Handle(request, cancellationToken);
                return result.ToResults();
            });
        }

        return Task.CompletedTask;
    }

    public Task MappRoutes<TRequest, TResponse>(EntityActionRequestContext entityActionRequestContext)
    {
        var entityGroup = entityActionRequestContext.EntityRouteGroupBuider;
        var actionName = entityActionRequestContext.EntityActionMetadata.ActionName;
        var keyProperty = entityActionRequestContext.EntityActionMetadata.KeyProperty;
        var mappedMethods = MapHttpMethods(entityActionRequestContext.EntityActionMetadata.HttpMethod);

        var app = entityActionRequestContext.App;

        if (keyProperty is not null)
        {
            entityGroup.MapMethods($"{{key}}/{actionName}", mappedMethods, async (
            [FromBody] TRequest request, QueryRequest<TRequest> queryRequest,
            string key,
            HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                request ??= queryRequest.Request!;
                var keyValue = key.Parse(keyProperty.PropertyType);
                keyProperty.SetValue(request, keyValue);

                var serviceProvider = httpContext.RequestServices;
                var handlerObj = ActivatorUtilities.CreateInstance(serviceProvider
                    , entityActionRequestContext.EntityActionMetadata.TargetType);

                if (handlerObj is not IOperationHandler<TRequest, TResponse> handler)
                    throw new InvalidOperationException("Invalid Handler");

                if (handler is null)
                    throw new InvalidOperationException("Invalid Handler");

                var result = await handler.Handle(request, cancellationToken);
                return result.ToResults();
            });
        }
        else
        {
            entityGroup.MapMethods(actionName, mappedMethods, async (
            [FromBody] TRequest? request, QueryRequest<TRequest> queryRequest,
            HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                request ??= queryRequest.Request!;

                var serviceProvider = httpContext.RequestServices;
                var handlerObj = ActivatorUtilities.CreateInstance(serviceProvider
                    , entityActionRequestContext.EntityActionMetadata.TargetType);

                if (handlerObj is not IOperationHandler<TRequest, TResponse> handler)
                    throw new InvalidOperationException("Invalid Handler");

                if (handler is null)
                    throw new InvalidOperationException("Invalid Handler");

                var result = await handler.Handle(request, cancellationToken);
                return result.ToResults();
            });
        }

        return Task.CompletedTask;
    }
}

public class DefaultEntityActionRequestHandler<TRequest> : DefaultEntityActionRequestHandler,
    IEntityActionRequestHandler<TRequest>
{
    public Task MappRoutes(EntityActionRequestContext entityActionRequestContext)
        => MappRoutes<TRequest>(entityActionRequestContext);
}

public class DefaultEntityActionRequestHandler<TRequest, TResponse> : DefaultEntityActionRequestHandler,
    IEntityActionRequestHandler<TRequest, TResponse>
{
    public Task MappRoutes(EntityActionRequestContext entityActionRequestContext)
        => MappRoutes<TRequest, TResponse>(entityActionRequestContext);
}


[Obsolete]
public class EntityOperationRequestHandler<TODataViewModel, TKey, TRequest> : IHttpRequestHandler
where TODataViewModel : class
{
    private readonly ODataMetadataContainer _container;
    private readonly EntityOperationMetadata _metadata;

    public EntityOperationRequestHandler(ODataMetadataContainer container
        , EntityOperationMetadata metadata)
    {
        _container = container;
        _metadata = metadata;
    }
    public ODataMetadataContainer Container => _container;

    private static Action<TRequest, TKey>? _keySetter = null;

    public Task MappRouters(WebApplication webApplication)
    {
        var entityGroup = _container.CreateOrGetEntityOperationGroup(webApplication, _metadata);
        var isKeyed = _metadata.IsKeyedOperation<TKey>(out var keyProp);
        var pattern = isKeyed
            ? $"/{{key}}/{_metadata.OperationName}"
            : $"/{_metadata.OperationName}";

        if (isKeyed && keyProp is not null)
            _keySetter = ObjectUtils.BuildSetter<TRequest, TKey>(keyProp);

        var method = _metadata.OperationType switch
        {
            OperationType.Action => "POST",
            OperationType.Function => "GET",
            _ => throw new InvalidOperationException("Invalid Operation Type")
        };

        if (isKeyed)
        {
            entityGroup.MapMethods(pattern, [method], async (
                HttpRequest httpRequest,
                TKey key,
                [FromServices] IEntityOperationHandler<TODataViewModel, TRequest> requestHandler
                , CancellationToken cancellationToken) =>
            {
                TRequest request = await this.ParseRequest<TRequest>(httpRequest)!;
                if (request is not null)
                    _keySetter?.Invoke(request, key!);

                var result = await requestHandler.Handle(request, cancellationToken);
                return result.ToResults();
            });
        }
        else
        {
            entityGroup.MapMethods(pattern, [method], async (
                HttpRequest httpRequest,
                [FromServices] IEntityOperationHandler<TODataViewModel, TRequest> requestHandler
                , CancellationToken cancellationToken) =>
            {
                TRequest request = await this.ParseRequest<TRequest>(httpRequest)!;
                var result = await requestHandler.Handle(request, cancellationToken);
                return result.ToResults();
            });
        }

        return Task.CompletedTask;
    }
}


public class EntityOperationRequestHandler<TODataViewModel, TKey, TRequest, TResponse> : IHttpRequestHandler
where TODataViewModel : class
{
    private readonly ODataMetadataContainer _container;
    private readonly EntityOperationMetadata _metadata;

    public EntityOperationRequestHandler(ODataMetadataContainer container
        , EntityOperationMetadata metadata)
    {
        _container = container;
        _metadata = metadata;
    }

    private static Action<TRequest, TKey>? _keySetter = null;

    public Task MappRouters(WebApplication webApplication)
    {
        var entityGroup = _container.CreateOrGetEntityOperationGroup(webApplication, _metadata);
        var isKeyed = _metadata.IsKeyedOperation<TKey>(out var keyProp);
        var pattern = isKeyed
            ? $"/{{key}}/{_metadata.OperationName}"
            : $"/{_metadata.OperationName}";

        if (isKeyed && keyProp is not null)
            _keySetter = ObjectUtils.BuildSetter<TRequest, TKey>(keyProp);

        var method = _metadata.OperationType switch
        {
            OperationType.Action => "POST",
            OperationType.Function => "GET",
            _ => throw new InvalidOperationException("Invalid Operation Type")
        };

        if (isKeyed)
        {
            entityGroup.MapMethods(pattern, [method], async (
                [FromServices] IEntityOperationHandler<TODataViewModel, TRequest, TResponse> requestHandler
                , HttpRequest httpRequest
                , TKey key
                , CancellationToken cancellationToken) =>
            {
                TRequest request = await this.ParseRequest<TRequest>(httpRequest)!;
                if (request is not null)
                    _keySetter?.Invoke(request, key!);

                var result = await requestHandler.Handle(request, cancellationToken);
                return result.ToResults();
            });
        }
        else
        {
            entityGroup.MapMethods(pattern, [method], async (
                HttpRequest httpRequest,
                [FromServices] IEntityOperationHandler<TODataViewModel, TRequest, TResponse> requestHandler
                , CancellationToken cancellationToken) =>
            {
                TRequest request = await this.ParseRequest<TRequest>(httpRequest)!;
                var result = await requestHandler.Handle(request, cancellationToken);
                return result.ToResults();
            });
        }

        return Task.CompletedTask;
    }
}


