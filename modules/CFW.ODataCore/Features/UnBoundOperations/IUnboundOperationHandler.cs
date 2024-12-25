namespace CFW.ODataCore.Features.UnBoundOperations;

public interface IUnboundOperationHandler<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IUnboundOperationHandler<TRequest>
{
    Task<Result> Handle(TRequest request, CancellationToken cancellationToken);
}
