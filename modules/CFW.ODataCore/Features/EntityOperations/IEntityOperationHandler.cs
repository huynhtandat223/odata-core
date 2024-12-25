namespace CFW.ODataCore.Features.BoundOperations;

public interface IEntityOperationHandler<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IEntityOperationHandler<TRequest>
{
    Task<Result> Handle(TRequest request, CancellationToken cancellationToken);
}