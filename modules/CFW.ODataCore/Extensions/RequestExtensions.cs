using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Extensions;

public static class RequestExtensions
{
    public static async Task<ActionResult> Execute<TRequest, TResponse>(this ODataController controller,
        TRequest request, CancellationToken cancellationToken)
    {
        if (!controller.ModelState.IsValid)
            return controller.BadRequest(controller.ModelState);
        if (typeof(TResponse) == typeof(Result))
        {
            var handler = controller.HttpContext.RequestServices.GetRequiredService<IODataOperationHandler<TRequest>>();
            var result = await handler.Execute(request, cancellationToken);
            return result.ToActionResult();
        }
        else
        {
            var handler = controller.HttpContext.RequestServices.GetRequiredService<IODataOperationHandler<TRequest, TResponse>>();
            var result = await handler.Execute(request, cancellationToken);
            return result.ToActionResult();
        }
    }
}
