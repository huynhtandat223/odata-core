using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;

namespace CFW.ODataCore.Features.EntitySets;

public class EntitySetsConvention : Attribute, IControllerModelConvention
{
    private ODataMetadataContainer _container;

    public EntitySetsConvention(ODataMetadataContainer container)
    {
        _container = container;
    }

    public void Apply(ControllerModel controller)
    {
        var metadataEntity = _container.EntityMetadataList.FirstOrDefault(x => x.ControllerType == controller.ControllerType);
        if (metadataEntity is null)
            return;

        var entitySet = metadataEntity.Container.EdmModel.EntityContainer.FindEntitySet(metadataEntity.Name);
        var withoutKeyTemplate = new ODataPathTemplate(new EntitySetsTemplate(entitySet, ignoreKeyTemplates: true));
        var withKeyTemplate = new ODataPathTemplate(new EntitySetsTemplate(entitySet, ignoreKeyTemplates: false));
        var routePrefix = metadataEntity.Container.RoutePrefix;
        var edmModel = metadataEntity.Container.EdmModel;
        var routingAttribute = metadataEntity.SetupAttributes.OfType<ODataEntitySetAttribute>().Single();
        var allowMethods = routingAttribute.AllowMethods ?? Enum.GetValues<ODataMethod>();
        var authorizeAttrs = metadataEntity.SetupAttributes.OfType<ODataAuthorizeAttribute>();
        var anonymousAttrs = metadataEntity.SetupAttributes.OfType<ODataAllowAnonymousAttribute>();


        //QUERY
        if (allowMethods.Contains(ODataMethod.Query))
        {
            var queryAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Query));
            queryAction.AddSelector(HttpMethod.Get.Method, routePrefix, edmModel, withoutKeyTemplate);
            AddAuthorizationInfo(queryAction, authorizeAttrs, anonymousAttrs, ODataMethod.Query);
        }

        //GET
        if (allowMethods.Contains(ODataMethod.GetByKey))
        {
            var getAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Get));
            getAction.AddSelector(HttpMethod.Get.Method, routePrefix, edmModel, withKeyTemplate);
            AddAuthorizationInfo(getAction, authorizeAttrs, anonymousAttrs, ODataMethod.GetByKey);
        }


        //POST (create)
        if (allowMethods.Contains(ODataMethod.PostCreate))
        {
            var postAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Post));
            postAction.AddSelector(HttpMethod.Post.Method, routePrefix, edmModel, withoutKeyTemplate);
            AddAuthorizationInfo(postAction, authorizeAttrs, anonymousAttrs, ODataMethod.PostCreate);
        }

        //DELETE
        if (allowMethods.Contains(ODataMethod.Delete))
        {
            var deleteAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Delete));
            deleteAction.AddSelector(HttpMethod.Delete.Method, routePrefix, edmModel, withKeyTemplate);
            AddAuthorizationInfo(deleteAction, authorizeAttrs, anonymousAttrs, ODataMethod.Delete);
        }

        //PATCH
        if (allowMethods.Contains(ODataMethod.PatchUpdate))
        {
            var patchAction = controller.Actions.Single(a => a.ActionName == nameof(EntitySetsController<RefODataViewModel, int>.Patch));
            patchAction.AddSelector(HttpMethod.Patch.Method, routePrefix, edmModel, withKeyTemplate);
            AddAuthorizationInfo(patchAction, authorizeAttrs, anonymousAttrs, ODataMethod.PatchUpdate);
        }
    }

    private void AddAuthorizationInfo(ActionModel actionModel
        , IEnumerable<ODataAuthorizeAttribute> authorizeAttrs
        , IEnumerable<ODataAllowAnonymousAttribute> anonymousAttributes
        , ODataMethod method)
    {
        var authAttr = authorizeAttrs.SingleOrDefault(x => x.ApplyMethods is not null
                && x.ApplyMethods.Contains(method));

        if (authAttr is not null)
        {
            var authorizeFilter = new AuthorizeFilter([authAttr]);
            actionModel.Filters.Add(authorizeFilter);
            return;
        }

        var allowAnonymous = anonymousAttributes.SingleOrDefault(x => x.ApplyMethods is not null
                && x.ApplyMethods.Contains(method));
        if (allowAnonymous is not null)
        {
            var allowAnonymousFilter = new AllowAnonymousFilter();
            actionModel.Filters.Add(allowAnonymousFilter);
            return;
        }

        var defaultAuthorize = authorizeAttrs.SingleOrDefault(x => x.ApplyMethods is null);
        if (defaultAuthorize is not null)
        {
            var authorizeFilter = new AuthorizeFilter([defaultAuthorize]);
            actionModel.Filters.Add(authorizeFilter);
            return;
        }
    }

}
