using CFW.ODataCore.Features.BoundActions;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.Features.Shared;

[Obsolete("Merge all actions and functions")]
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


