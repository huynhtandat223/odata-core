using CFW.ODataCore.Features.EntitySets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Features.EntitySets.Handlers;

public interface IRequestHandler<TODataViewModel, TKey>
   where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<ActionResult> HandleQuery(EntitySetsController<TODataViewModel, TKey> controller, ODataQueryOptions<TODataViewModel> options
        , CancellationToken cancellationToken);

    Task<ActionResult<TODataViewModel?>> HandleGet(EntitySetsController<TODataViewModel, TKey> controller
        , TKey key, ODataQueryOptions<TODataViewModel> options
        , CancellationToken cancellationToken);

    Task<ActionResult<TODataViewModel>> HandlePost(EntitySetsController<TODataViewModel, TKey> controller
        , TODataViewModel viewModel, CancellationToken cancellationToken);

    Task<ActionResult> HandlePatch(EntitySetsController<TODataViewModel, TKey> controller
        , TKey key, Delta<TODataViewModel> delta, CancellationToken cancellationToken);

    Task<ActionResult> HandleDelete(EntitySetsController<TODataViewModel, TKey> controller
        , TKey key, CancellationToken cancellationToken);
}

public class DefaultRequestHandler<TODataViewModel, TKey> : IRequestHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> HandleDelete(EntitySetsController<TODataViewModel, TKey> controller, TKey key, CancellationToken cancellationToken)
    {
        if (!controller.ModelState.IsValid)
            return controller.BadRequest(controller.ModelState);

        var handler = controller.HttpContext.RequestServices.GetRequiredService<IDeleteHandler<TODataViewModel, TKey>>();
        var result = await handler.Delete(key, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult<TODataViewModel?>> HandleGet(EntitySetsController<TODataViewModel, TKey> controller, TKey key, ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        if (!controller.ModelState.IsValid)
            return controller.BadRequest(controller.ModelState);

        var handler = controller.HttpContext.RequestServices.GetRequiredService<IGetByKeyHandler<TODataViewModel, TKey>>();
        var result = await handler.Get(key, options, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult> HandlePatch(EntitySetsController<TODataViewModel, TKey> controller, TKey key, Delta<TODataViewModel> delta, CancellationToken cancellationToken)
    {
        if (!controller.ModelState.IsValid)
            return controller.BadRequest(controller.ModelState);

        var handler = controller.HttpContext.RequestServices.GetRequiredService<IPatchHandler<TODataViewModel, TKey>>();
        var result = await handler.Patch(key, delta, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult<TODataViewModel>> HandlePost(EntitySetsController<TODataViewModel, TKey> controller, TODataViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!controller.ModelState.IsValid)
            return controller.BadRequest(controller.ModelState);

        var handler = controller.HttpContext.RequestServices.GetRequiredService<ICreateHandler<TODataViewModel, TKey>>();
        var result = await handler.Create(viewModel, cancellationToken);
        return result.ToActionResult();
    }

    public async Task<ActionResult> HandleQuery(EntitySetsController<TODataViewModel, TKey> controller
        , ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        if (!controller.ModelState.IsValid)
            return controller.BadRequest(controller.ModelState);

        var queryHandler = controller.HttpContext.RequestServices.GetRequiredService<IQueryHandler<TODataViewModel, TKey>>();

        var result = await queryHandler.Query(options, cancellationToken);
        return result.ToActionResult();
    }
}
