using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.BoundOperations;

public class EntityBoundOprationsController<TODataViewModel, TKey, TRequest, TResponse> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> ExecuteBoundFunction(
        [FromServices] IEntityOperationHandler<TRequest, TResponse> requestHandler,
        [FromQuery] TRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await requestHandler.Handle(request, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult> ExecuteBoundAction(
        [FromServices] IEntityOperationHandler<TRequest, TResponse> requestHandler,
        [BodyBinder] TRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await requestHandler.Handle(request, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult> ExecuteNonResponseBoundAction(
        [FromServices] IEntityOperationHandler<TRequest> requestHandler,
        [BodyBinder] TRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await requestHandler.Handle(request, cancellationToken);
        return result.ToActionResult();
    }
}
