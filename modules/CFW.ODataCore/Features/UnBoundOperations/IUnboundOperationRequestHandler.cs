using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.UnBoundOperations;

public interface IUnboundOperationRequestHandler<TRequest, TResponse>
{
    Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken);
}

public class DefaultUnboundOperationRequestHandler<TRequest, TResponse>
    : IUnboundOperationRequestHandler<TRequest, TResponse>
{
    public async Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken)
    {
        return await controller.Execute<TRequest, TResponse>(request, cancellationToken);
    }
}


