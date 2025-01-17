﻿namespace CFW.ODataCore.Intefaces;

/// <summary>
/// Support handle type for multi entity.
/// </summary>
/// <typeparam name="TViewModelType"></typeparam>
/// <typeparam name="TRequest"></typeparam>
public interface IEntityOperationHandler<TViewModelType, TRequest>
    where TViewModelType : class
{
    Task<Result> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IEntityOperationHandler<TViewModelType, TRequest, TResponse>
    where TViewModelType : class
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}
