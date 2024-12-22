using CFW.ODataCore.Features.BoundActions;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.Features.Shared;

[Obsolete("Merge all actions and functions")]
public interface IBoundActionRequestHandler<TODataViewModel, TKey, TRequest, TResponse>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<ActionResult> Handle(BoundActionsController<TODataViewModel, TKey, TRequest, TResponse> controller
        , TRequest request, CancellationToken cancellationToken);
}


