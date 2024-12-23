﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.BoundOperations;

public interface IBoundOperationRequestHandler<TODataViewModel, TKey, TRequest, TResponse>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken);
}

public class DefaultBoundOperationRequestHandler<TODataViewModel, TKey, TRequest, TResponse>
    : IBoundOperationRequestHandler<TODataViewModel, TKey, TRequest, TResponse>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken)
    {
        return await controller.Execute<TRequest, TResponse>(request, cancellationToken);
    }
}