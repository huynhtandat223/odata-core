using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityDeleteController<TODataViewModel, TKey> : ODataController
    where TODataViewModel : IODataViewModel<TKey>
{
    public async Task<ActionResult<TODataViewModel>> Delete(TKey key, [FromServices] IEntityDeleteHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        var result = await handler.Handle(key, cancellationToken);
        return result.ToActionResult();
    }
}
