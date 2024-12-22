using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.BoundOperations;

public interface IBoundOperationRequestHandler<TODataViewModel, TKey, TRequest, TResponse>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken);
}

public class DefaultBoundOperationRequestHandler<TViewModel, TKey, TRequest, TResponse>
    : DefaultActionRequestHandler<TRequest, TResponse>, IBoundOperationRequestHandler<TViewModel, TKey, TRequest, TResponse>
    where TViewModel : class, IODataViewModel<TKey>
{

}
