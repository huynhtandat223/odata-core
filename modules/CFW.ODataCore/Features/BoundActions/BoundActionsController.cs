using CFW.ODataCore.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.BoundActions;

[BoundActionsConvention]
public class BoundActionsController<TODataViewModel, TKey, TRequest, TResponse> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> Execute(
        [FromServices] IBoundActionRequestHandler<TODataViewModel, TKey, TRequest, TResponse> requestHandler,
        [BodyBinder] TRequest request, CancellationToken cancellationToken)
    {
        var result = await requestHandler.Handle(this, request, cancellationToken);
        return result;
    }

}
