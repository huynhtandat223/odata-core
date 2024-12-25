using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.UnBoundOperations;

public class UnboundOprationsController<TRequest, TResponse> : ODataController
{
    public async Task<ActionResult> ExecuteUnBoundFunction(
        [FromServices] IUnboundOperationHandler<TRequest, TResponse> requestHandler,
        [FromQuery] TRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await requestHandler.Handle(request, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult> ExecuteUnboundAction(
        [FromServices] IUnboundOperationHandler<TRequest, TResponse> requestHandler,
        [BodyBinder] TRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await requestHandler.Handle(request, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult> ExecuteNoResponseAction(
        [FromServices] IUnboundOperationHandler<TRequest> requestHandler,
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
