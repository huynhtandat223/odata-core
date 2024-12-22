using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.BoundOperations;

public class BoundOperationsController<TODataViewModel, TKey, TRequest, TResponse> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> ExecuteBoundFunction(
        [FromServices] IBoundOperationRequestHandler<TODataViewModel, TKey, TRequest, TResponse> requestHandler,
        [FromQuery] TRequest request, CancellationToken cancellationToken)
    {
        var result = await requestHandler.Handle(this, request, cancellationToken);
        return result;
    }

    public async Task<ActionResult> ExecuteBoundAction(
        [FromServices] IBoundOperationRequestHandler<TODataViewModel, TKey, TRequest, TResponse> requestHandler,
        [BodyBinder] TRequest request, CancellationToken cancellationToken)
    {
        var result = await requestHandler.Handle(this, request, cancellationToken);
        return result;
    }

}
