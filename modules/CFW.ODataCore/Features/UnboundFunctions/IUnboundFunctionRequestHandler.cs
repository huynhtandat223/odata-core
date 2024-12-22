using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.UnboundFunctions;

public interface IUnboundFunctionRequestHandler<TRequest, TResponse>
{
    Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken);
}
