using CFW.ODataCore.Core;
using CFW.ODataCore.Core.Templates;
using CFW.ODataCore.Extensions;
using CFW.ODataCore.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
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
        if (metadataEntity.AllowMethods.Contains(AllowMethod.Query))
        {
            var queryAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Query));
            queryAction.AddSelector(HttpMethod.Get.Method, routePrefix, edmModel, withoutKeyTemplate);
            AddAuthorizationInfo(queryAction, metadataEntity);
        }

        //GET
        if (metadataEntity.AllowMethods.Contains(AllowMethod.GetByKey))
        {
            var getAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Get));
            getAction.AddSelector(HttpMethod.Get.Method, routePrefix, edmModel, withKeyTemplate);
            AddAuthorizationInfo(getAction, metadataEntity);
        }


        //POST (create)
        if (metadataEntity.AllowMethods.Contains(AllowMethod.PostCreate))
        {
            var postAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Post));
            postAction.AddSelector(HttpMethod.Post.Method, routePrefix, edmModel, withoutKeyTemplate);
            AddAuthorizationInfo(postAction, metadataEntity);
        }

        //DELETE
        if (metadataEntity.AllowMethods.Contains(AllowMethod.Delete))
        {
            var deleteAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Delete));
            deleteAction.AddSelector(HttpMethod.Delete.Method, routePrefix, edmModel, withKeyTemplate);
            AddAuthorizationInfo(deleteAction, metadataEntity);
        }

        //PATCH
        if (metadataEntity.AllowMethods.Contains(AllowMethod.PatchUpdate))
        {
            var patchAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Patch));
            patchAction.AddSelector(HttpMethod.Patch.Method, routePrefix, edmModel, withKeyTemplate);
            AddAuthorizationInfo(patchAction, metadataEntity);
        }

    }

    private void AddAuthorizationInfo(ActionModel actionModel, ODataMetadataEntity metadataEntity)
    {
        if (metadataEntity.AllowAnonymousAttribute is not null)
        {
            var allowAnonymousFilter = new AllowAnonymousFilter();
            actionModel.Filters.Add(allowAnonymousFilter);
        }

        if (metadataEntity.AuthorizeAttribute is not null)
        {
            var authorizeFilter = new AuthorizeFilter([metadataEntity.AuthorizeAttribute]);
            actionModel.Filters.Add(authorizeFilter);
        }
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