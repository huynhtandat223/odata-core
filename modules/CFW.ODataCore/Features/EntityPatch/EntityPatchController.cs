using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityPatchController<TODataViewModel, TKey> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult<TODataViewModel>> Patch(TKey key, [FromBody] Delta<TODataViewModel> delta
        , [FromServices] IEntityPatchHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        var result = await handler.Handle(key, delta, cancellationToken);
        return result.ToActionResult();
    }
}
