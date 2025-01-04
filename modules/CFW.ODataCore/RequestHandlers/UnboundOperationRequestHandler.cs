using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.ODataMetadata;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RequestHandlers;

public class UnboundOperationRequestHandler<TKey, TRequest> : IHttpRequestHandler
{
    private readonly ODataMetadataContainer _container;
    private readonly UnboundOperationMetadata _metadata;
    public UnboundOperationRequestHandler(ODataMetadataContainer container
        , UnboundOperationMetadata metadata)
    {
        _container = container;
        _metadata = metadata;
    }

    private static Action<TRequest, TKey>? _keySetter = null;

    public Task MappRouters(WebApplication webApplication)
    {
        var containerGroup = _container.CreateOrGetContainerRoutingGroup(webApplication);
        var isKeyed = _metadata.IsKeyedOperation(out var keyProp);
        var pattern = isKeyed
            ? $"/{_metadata.OperationName}/{{key}}"
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
            containerGroup.MapMethods(pattern, [method], async (
                HttpRequest httpRequest,
                TKey key,
                [FromServices] IUnboundOperationHandler<TRequest> requestHandler
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
            containerGroup.MapMethods(pattern, [method], async (
                HttpRequest httpRequest,
                [FromServices] IUnboundOperationHandler<TRequest> requestHandler
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

public class UnboundOperationRequestHandler<TKey, TRequest, TResponse> : IHttpRequestHandler
{
    private readonly ODataMetadataContainer _container;
    private readonly UnboundOperationMetadata _metadata;
    public UnboundOperationRequestHandler(ODataMetadataContainer container
        , UnboundOperationMetadata metadata)
    {
        _container = container;
        _metadata = metadata;
    }

    private static Action<TRequest, TKey>? _keySetter = null;

    public Task MappRouters(WebApplication webApplication)
    {
        var containerGroup = _container.CreateOrGetContainerRoutingGroup(webApplication);
        var isKeyed = _metadata.IsKeyedOperation(out var keyProp);
        var pattern = isKeyed
            ? $"/{_metadata.OperationName}/{{key}}"
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
            containerGroup.MapMethods(pattern, [method], async (
                [FromServices] IUnboundOperationHandler<TRequest, TResponse> requestHandler
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
            containerGroup.MapMethods(pattern, [method], async (
                HttpRequest httpRequest,
                [FromServices] IUnboundOperationHandler<TRequest, TResponse> requestHandler
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


