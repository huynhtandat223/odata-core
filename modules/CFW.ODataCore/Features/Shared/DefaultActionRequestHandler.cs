using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.Shared;

public interface IUnboundActionRequestHandler<TRequest, TResponse>
{
    Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken);
}

public class DefaultActionRequestHandler<TRequest, TResponse> : IUnboundActionRequestHandler<TRequest, TResponse>
{
    public async Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken)
    {
        if (!controller.ModelState.IsValid)
            return controller.BadRequest(controller.ModelState);

        if (typeof(TResponse) == typeof(Result))
        {
            var handler = controller.HttpContext.RequestServices.GetRequiredService<IODataActionHandler<TRequest>>();
            var result = await handler.Execute(request, cancellationToken);
            return result.ToActionResult();
        }
        else
        {
            var handler = controller.HttpContext.RequestServices.GetRequiredService<IODataActionHandler<TRequest, TResponse>>();
            var result = await handler.Execute(request, cancellationToken);
            return result.ToActionResult();
        }
    }
}


