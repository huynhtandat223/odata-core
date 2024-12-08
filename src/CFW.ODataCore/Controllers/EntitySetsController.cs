using CFW.ODataCore.Core;
using CFW.ODataCore.Core.Templates;
using CFW.ODataCore.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Routing.Template;

namespace CFW.ODataCore.Controllers;

public class RefODataViewModel : IODataViewModel<int>
{
    public int Id { get; set; }
}

public class EntitySetsConvention : Attribute, IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (!controller.ControllerType.IsGenericType || controller.ControllerType.GetGenericTypeDefinition() != typeof(EntitySetsController<,>))
            throw new InvalidOperationException("Controller must be a GenericController<TViewModel>.");

        var metadataEntity = ODataContainerCollection.Instance.GetMetadataEntity(controller.ControllerType);
        var entitySet = metadataEntity.Container.EdmModel.EntityContainer.FindEntitySet(metadataEntity.Name);

        var withoutKeyTemplate = new ODataPathTemplate(new EntitySetsTemplate(entitySet, ignoreKeyTemplates: true));
        var withKeyTemplate = new ODataPathTemplate(new EntitySetsTemplate(entitySet, ignoreKeyTemplates: false));
        var routePrefix = metadataEntity.Container.RoutePrefix;
        var edmModel = metadataEntity.Container.EdmModel;

        //QUERY
        var queryAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Query));
        queryAction.AddSelector(HttpMethod.Get.Method, routePrefix, edmModel, withoutKeyTemplate);

        //GET
        var getAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Get));
        getAction.AddSelector(HttpMethod.Get.Method, routePrefix, edmModel, withKeyTemplate);

        //POST (create)
        var postAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Post));
        postAction.AddSelector(HttpMethod.Post.Method, routePrefix, edmModel, withoutKeyTemplate);

        //DELETE
        var deleteAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Delete));
        deleteAction.AddSelector(HttpMethod.Delete.Method, routePrefix, edmModel, withKeyTemplate);

        //PATCH
        var patchAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Patch));
        patchAction.AddSelector(HttpMethod.Patch.Method, routePrefix, edmModel, withKeyTemplate);
    }
}


[EntitySetsConvention]
public class EntitySetsController<TODataViewModel, TKey> : ODataController
    where TODataViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult<IQueryable<TODataViewModel>>> Query(ODataQueryOptions<TODataViewModel> options
        , [FromServices] ApiHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        var query = await handler.Query(options, cancellationToken);
        return Ok(query);
    }

    public async Task<ActionResult<TODataViewModel?>> Get(TKey key,
        ODataQueryOptions<TODataViewModel> options
        , [FromServices] ApiHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        var entity = await handler.Get(key, options, cancellationToken);
        return Ok(entity);
    }

    public async Task<ActionResult<TODataViewModel>> Post([FromBody] TODataViewModel viewModel
        , [FromServices] ApiHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var newViewModel = await handler.Create(viewModel, cancellationToken);

        return Created(newViewModel);
    }

    public async Task<ActionResult> Patch(TKey key, [FromBody] Delta<TODataViewModel> delta
        , [FromServices] ApiHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        var updatedModel = await handler.Patch(key, delta, cancellationToken);
        return Ok(updatedModel);
    }

    public async Task<ActionResult> Delete(TKey key
        , [FromServices] ApiHandler<TODataViewModel, TKey> handler
        , CancellationToken cancellationToken)
    {
        await handler.Delete(key, cancellationToken);
        return Ok();
    }
}