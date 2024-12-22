using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.UnBoundActions;

public class UnboundActionsController<TRequest, TResponse> : ODataController
{
    public async Task<ActionResult> Execute(
        [FromServices] IUnboundActionRequestHandler<TRequest, TResponse> requestHandler,
        [BodyBinder] TRequest request, CancellationToken cancellationToken)
    {
        var result = await requestHandler.Handle(this, request, cancellationToken);
        return result;
    }
}
