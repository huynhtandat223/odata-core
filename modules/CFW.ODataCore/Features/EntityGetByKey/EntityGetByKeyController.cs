using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityGetByKeyController<TODataViewModel, TKey> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult<TODataViewModel>> GetByKey(TKey key, ODataQueryOptions<TODataViewModel> options
        , [FromServices] IEntityGetByKeyHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        var result = await handler.Handle(key, options, cancellationToken);
        return result.ToActionResult();
    }
}
