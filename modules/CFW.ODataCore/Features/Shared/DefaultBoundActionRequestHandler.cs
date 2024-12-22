using CFW.ODataCore.Features.BoundActions;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.Features.Shared;

public interface IBoundActionRequestHandler<TODataViewModel, TKey, TRequest, TResponse>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<ActionResult> Handle(BoundActionsController<TODataViewModel, TKey, TRequest, TResponse> controller
        , TRequest request, CancellationToken cancellationToken);
}

public class DefaultBoundActionRequestHandler<TViewModel, TKey, TRequest, TResponse>
    : DefaultActionRequestHandler<TRequest, TResponse>, IBoundActionRequestHandler<TViewModel, TKey, TRequest, TResponse>
    where TViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> Handle(BoundActionsController<TViewModel, TKey, TRequest, TResponse> controller
        , TRequest request, CancellationToken cancellationToken)
    {
        return await base.Handle(controller, request, cancellationToken);
    }
}


