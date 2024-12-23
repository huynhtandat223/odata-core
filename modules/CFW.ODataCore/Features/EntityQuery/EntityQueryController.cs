using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityQueryController<TODataViewModel, TKey> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult<TODataViewModel>> Query(ODataQueryOptions<TODataViewModel> options
        , [FromServices] IEntityQueryHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        var result = await handler.Handle(options, cancellationToken);
        return result.ToActionResult();
    }
}
