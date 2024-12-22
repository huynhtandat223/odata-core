using CFW.ODataCore.Attributes;
using CFW.ODataCore.Features.Core;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;

namespace CFW.ODataCore.Features.UnBoundActions;

public class UnBoundActionsConvention : IControllerModelConvention
{
    private ODataMetadataContainer _container;

    public UnBoundActionsConvention(ODataMetadataContainer container)
    {
        _container = container;
    }

    public void Apply(ControllerModel controller)
    {
        var unboundAction = _container.UnBoundActions.FirstOrDefault(x => x.ControllerType == controller.ControllerType);
        if (unboundAction is null)
            return;

        var routePrefix = _container.RoutePrefix;
        var edmModel = _container.EdmModel;

        var boundActionName = unboundAction.UnboundActionAttribute.Name;

        var edmAction = edmModel.SchemaElements
            .OfType<IEdmAction>()
            .Single(x => !x.IsBound && x.Name == boundActionName);

        var requestType = unboundAction.RequestType;

        var controlerAction = controller.Actions
                .Single(a => a.ActionName == nameof(UnboundActionsController<object, object>.Execute));
        var template = new ODataPathTemplate(new UnboundActionsTemplate(edmAction));

        controlerAction.AddSelector(HttpMethod.Post.Method, routePrefix, edmModel, template);

        var authAttr = unboundAction.SetupAttributes.OfType<ODataAuthorizeAttribute>().SingleOrDefault();
        var anonymousAttr = unboundAction.SetupAttributes.OfType<ODataAllowAnonymousAttribute>().SingleOrDefault();

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
