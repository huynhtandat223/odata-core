namespace CFW.ODataCore.Intefaces;

public interface IOperationHandler<TRequest>
{
    Task<Result> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IOperationHandler<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}
