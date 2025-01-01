using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.ODataMetadata;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RequestHandlers;

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
                _keySetter?.Invoke(request, key!);
                var result = await requestHandler.Handle(request, cancellationToken);
                if (result.IsSuccess)
                    return Results.Ok();
                return Results.BadRequest(result.Message);
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
                if (result.IsSuccess)
                    return Results.Ok();
                return Results.BadRequest(result.Message);
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
                if (request is null)
                    return Results.BadRequest("Invalid request");

                _keySetter?.Invoke(request, key!);
                var result = await requestHandler.Handle(request, cancellationToken);
                if (result.IsSuccess)
                    return Results.Ok(result.Data);
                return Results.BadRequest(result.Message);
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
                if (request is null)
                    return Results.BadRequest("Invalid request");

                var result = await requestHandler.Handle(request, cancellationToken);

                if (result.IsSuccess)
                    return Results.Ok(result.Data);
                return Results.BadRequest(result.Message);
            });
        }

        return Task.CompletedTask;
    }
}


