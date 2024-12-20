namespace CFW.ODataCore.Features.BoundActions;

public interface IODataActionHandler<TRequest>
{
    Task<Result> Execute(TRequest request, CancellationToken cancellationToken);
}

public interface IODataActionHandler<TRequest, TResponse>
{
    Task<Result<TResponse>> Execute(TRequest request, CancellationToken cancellationToken);
}
