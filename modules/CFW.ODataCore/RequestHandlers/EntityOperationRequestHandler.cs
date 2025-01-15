using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.ODataMetadata;
using Microsoft.AspNetCore.Mvc;

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

public interface IEntityActionRequestHandler<TRequest>
{
}

public class DefaultEntityActionRequestHandler<TRequest> :
    IEntityActionRequestHandler<TRequest>
{
    public Task MappRoutes(EntityActionRequestContext entityRequestContext)
    {
        var entityGroup = entityRequestContext.EntityRouteGroupBuider;
        var actionName = entityRequestContext.EntityActionMetadata.ActionName;

        entityGroup.MapMethods(actionName, [entityRequestContext.EntityActionMetadata.HttpMethod.Method], async (
            TRequest request,
            [FromServices] IOperationHandler<TRequest> handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }
}

public class DefaultEntityActionRequestHandler<TRequest, TResponse> :
    IEntityActionRequestHandler<TRequest, TResponse>,
    IEntityActionRequestHandler<TRequest>
{
    public Task MappRoutes(EntityActionRequestContext entityRequestContext)
    {
        var entityGroup = entityRequestContext.EntityRouteGroupBuider;
        var actionName = entityRequestContext.EntityActionMetadata.ActionName;

        entityGroup.MapMethods(actionName, [entityRequestContext.EntityActionMetadata.HttpMethod.Method], async (
            TRequest request,
            [FromServices] IOperationHandler<TRequest, TResponse> handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }
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


