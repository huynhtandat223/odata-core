using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.UnBoundOperations;

public class UnboundOperationsController<TRequest, TResponse> : ODataController
{
    public async Task<ActionResult> ExecuteUnboundAction(
        [FromServices] IUnboundOperationRequestHandler<TRequest, TResponse> requestHandler,
        [BodyBinder] TRequest request, CancellationToken cancellationToken)
    {
        var result = await requestHandler.Handle(this, request, cancellationToken);
        return result;
    }

    public async Task<ActionResult> ExecuteUnboundFunction(
        [FromServices] IUnboundOperationRequestHandler<TRequest, TResponse> requestHandler
        , [FromQuery] TRequest request, CancellationToken cancellationToken)
    {
        var result = await requestHandler.Handle(this, request, cancellationToken);
        return result;
    }
}