using CFW.ODataCore.Attributes;
using CFW.ODataCore.Features.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using System.Reflection;

namespace CFW.ODataCore.Features.BoundActions;

public class BoundActionsConvention : IControllerModelConvention
{
    private List<ODataBoundActionMetadata> _boundActionMetadata;

    public BoundActionsConvention(List<ODataBoundActionMetadata> boundActionMetadata)
    {
        _boundActionMetadata = boundActionMetadata;
    }

    public void Apply(ControllerModel controller)
    {
        var boundActionMetadata = _boundActionMetadata.FirstOrDefault(x => x.BoundActionControllerType == controller.ControllerType);
        if (boundActionMetadata is null)
            return;

        var entitySet = boundActionMetadata.Container.EdmModel.EntityContainer.FindEntitySet(boundActionMetadata.BoundCollectionName);
        var entityFullName = entitySet.EntityType().FullName();
        var routePrefix = boundActionMetadata.Container.RoutePrefix;
        var edmModel = boundActionMetadata.Container.EdmModel;
        var boundActionName = boundActionMetadata.BoundActionAttribute.Name;

        var edmAction = edmModel.SchemaElements
            .OfType<IEdmAction>()
            .Where(x => x.IsBound && x.Parameters.First().Type.FullName() == entityFullName)
            .Single(x => x.Name == boundActionName);

        var requestType = boundActionMetadata.RequestType;
        var keyType = boundActionMetadata.KeyType;

        var hasKey = requestType.GetProperties()
            .Any(p => p.GetCustomAttribute<FromRouteAttribute>() is not null && p.PropertyType == keyType);

        var ignoreKeyTemplates = !hasKey;
        var template = new ODataPathTemplate(new BoundActionsTemplate(entitySet, ignoreKeyTemplates, edmAction));

        var controlerAction = controller.Actions
                .Single(a => a.ActionName == nameof(BoundActionsController<RefODataViewModel, int, object, object>.Execute));

        controlerAction.AddSelector(HttpMethod.Post.Method, routePrefix, edmModel, template);

        var authAttr = boundActionMetadata.SetupAttributes.OfType<ODataAuthorizeAttribute>().SingleOrDefault();
        var anonymousAttr = boundActionMetadata.SetupAttributes.OfType<ODataAllowAnonymousAttribute>().SingleOrDefault();

        if (authAttr is not null)
        {
            var authorizeFilter = new AuthorizeFilter([authAttr]);
            controlerAction.Filters.Add(authorizeFilter);
            return;
        }

        if (anonymousAttr is not null)
        {
            var allowAnonymousFilter = new AllowAnonymousFilter();
            controlerAction.Filters.Add(allowAnonymousFilter);
            return;
        }
    }
}
