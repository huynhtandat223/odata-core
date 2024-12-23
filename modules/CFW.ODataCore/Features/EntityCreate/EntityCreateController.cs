using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.EntityCreate;

public class EntityCreateController<TODataViewModel, TKey> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult<TODataViewModel>> Post([FromBody] TODataViewModel viewModel
        , [FromServices] IEntityCreateHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        var result = await handler.Handle(viewModel, cancellationToken);
        return result.ToActionResult();
    }
}
