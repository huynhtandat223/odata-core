using CFW.ODataCore.Controllers.Conventions;
using CFW.ODataCore.Extensions;
using CFW.ODataCore.Handlers;
using CFW.ODataCore.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Controllers;

[EntitySetsConvention]
public class EntitySetsController<TODataViewModel, TKey> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> Query(ODataQueryOptions<TODataViewModel> options
        , [FromServices] IQueryHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await handler.Query(options, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult<TODataViewModel?>> Get(TKey key,
        ODataQueryOptions<TODataViewModel> options
        , [FromServices] IGetByKeyHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await handler.Get(key, options, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult<TODataViewModel>> Post([FromBody] TODataViewModel viewModel
        , [FromServices] ICreateHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await handler.Create(viewModel, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult> Patch(TKey key, [FromBody] Delta<TODataViewModel> delta
        , [FromServices] IPatchHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await handler.Patch(key, delta, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult> Delete(TKey key
        , [FromServices] IDeleteHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await handler.Delete(key, cancellationToken);
        return result.ToActionResult();
    }
}