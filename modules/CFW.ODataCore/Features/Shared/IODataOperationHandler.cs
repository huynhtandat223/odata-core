namespace CFW.ODataCore.Features.Shared;

public interface IODataOperationHandler<TRequest, TResponse>
{
    Task<Result<TResponse>> Execute(TRequest request, CancellationToken cancellationToken);
}

public interface IODataOperationHandler<TRequest>
{
    Task<Result> Execute(TRequest request, CancellationToken cancellationToken);
}
