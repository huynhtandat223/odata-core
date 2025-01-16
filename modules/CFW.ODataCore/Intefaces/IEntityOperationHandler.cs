namespace CFW.ODataCore.Intefaces;

public interface IOperationHandler<TRequest>
{
    Task<Result> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IOperationHandler<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Support handle type for multi entity.
/// </summary>
/// <typeparam name="TViewModelType"></typeparam>
/// <typeparam name="TRequest"></typeparam>
[Obsolete]
public interface IEntityOperationHandler<TViewModelType, TRequest>
    where TViewModelType : class
{
    Task<Result> Handle(TRequest request, CancellationToken cancellationToken);
}

[Obsolete]
public interface IEntityOperationHandler<TViewModelType, TRequest, TResponse>
    where TViewModelType : class
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}
