using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.UnboundFunctions;

public class UnboundFunctionsController<TRequest, TResponse> : ODataController
{
    public async Task<ActionResult> Execute(
        [FromServices] IUnboundFunctionRequestHandler<TRequest, TResponse> requestHandler
        , [FromQuery] TRequest request, CancellationToken cancellationToken)
    {
        var result = await requestHandler.Handle(this, request, cancellationToken);
        return result;
    }
}
