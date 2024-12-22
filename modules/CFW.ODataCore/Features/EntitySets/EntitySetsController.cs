using CFW.ODataCore.Features.EntitySets.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.EntitySets;

public class EntitySetsController<TODataViewModel, TKey> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> Query(ODataQueryOptions<TODataViewModel> options
        , [FromServices] IRequestHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
        => await handler.HandleQuery(this, options, cancellationToken);

    public async Task<ActionResult<TODataViewModel?>> Get(TKey key,
        ODataQueryOptions<TODataViewModel> options
        , [FromServices] IRequestHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    => await handler.HandleGet(this, key, options, cancellationToken);


    public async Task<ActionResult<TODataViewModel>> Post([FromBody] TODataViewModel viewModel
        , [FromServices] IRequestHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    => await handler.HandlePost(this, viewModel, cancellationToken);


    public async Task<ActionResult> Patch(TKey key, [FromBody] Delta<TODataViewModel> delta
        , [FromServices] IRequestHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    => await handler.HandlePatch(this, key, delta, cancellationToken);

    public async Task<ActionResult> Delete(TKey key
        , [FromServices] IRequestHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    => await handler.HandleDelete(this, key, cancellationToken);
}