using CFW.ODataCore.Core;
using CFW.ODataCore.Extensions;
using CFW.ODataCore.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Controllers;

[EntitySetsConvention]
public class EntitySetsController<TODataViewModel, TKey> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult<IQueryable<TODataViewModel>>> Query(ODataQueryOptions<TODataViewModel> options
        , [FromServices] ApiHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var query = await handler.Query(options, cancellationToken);
        return Ok(query);
    }

    public async Task<ActionResult<TODataViewModel?>> Get(TKey key,
        ODataQueryOptions<TODataViewModel> options
        , [FromServices] ApiHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var entity = await handler.Get(key, options, cancellationToken);
        if (entity == null)
            return NotFound();

        return Ok(entity);
    }

    public async Task<ActionResult<TODataViewModel>> Post([FromBody] TODataViewModel viewModel
        , [FromServices] ICreateHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var newViewModel = await handler.Create(viewModel, cancellationToken);
        return newViewModel.ToActionResult();
    }

    public async Task<ActionResult> Patch(TKey key, [FromBody] Delta<TODataViewModel> delta
        , [FromServices] ApiHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updatedModel = await handler.Patch(key, delta, cancellationToken);
        return Updated(updatedModel);
    }

    public async Task<ActionResult> Delete(TKey key
        , [FromServices] ApiHandler<TODataViewModel, TKey> handler
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