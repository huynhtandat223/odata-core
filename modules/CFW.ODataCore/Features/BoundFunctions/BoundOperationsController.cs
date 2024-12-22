using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.BoundActions;

public class BoundOperationsController<TODataViewModel, TKey, TRequest, TResponse> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> Execute(
        [FromServices] IBoundOperationRequestHandler<TODataViewModel, TKey, TRequest, TResponse> requestHandler,
        [FromQuery] TRequest request, CancellationToken cancellationToken)
    {
        var result = await requestHandler.Handle(this, request, cancellationToken);
        return result;
    }

}
