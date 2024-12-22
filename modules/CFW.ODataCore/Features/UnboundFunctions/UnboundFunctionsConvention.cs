using CFW.ODataCore.Features.Core;
using CFW.ODataCore.Features.Shared;
using CFW.ODataCore.Features.UnBoundActions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;

namespace CFW.ODataCore.Features.UnboundFunctions;

public class UnboundFunctionsConvention : IControllerModelConvention
{
    private readonly ODataMetadataContainer _container;

    public UnboundFunctionsConvention(ODataMetadataContainer container)
    {
        _container = container;
    }

    public void Apply(ControllerModel controller)
    {
        var metadata = _container.UnboundFunctions.FirstOrDefault(x => x.ControllerType == controller.ControllerType);
        if (metadata == null)
            return;

        var routePrefix = _container.RoutePrefix;
        var edmModel = _container.EdmModel;

        var name = metadata.RoutingAttribute.Name;

        var edmAction = edmModel.SchemaElements
            .OfType<IEdmFunction>()
            .Single(x => !x.IsBound && x.Name == name);

        var requestType = metadata.RequestType;

        var controlerAction = controller.Actions
                .Single(a => a.ActionName == nameof(UnboundFunctionsController<object, object>.Execute));
        var template = new ODataPathTemplate(new UnboundOperationTemplate(edmAction));

        controlerAction.AddSelector(HttpMethod.Get.Method, routePrefix, edmModel, template);

        var authAttr = metadata.SetupAttributes.OfType<ODataAuthorizeAttribute>().SingleOrDefault();
        var anonymousAttr = metadata.SetupAttributes.OfType<ODataAllowAnonymousAttribute>().SingleOrDefault();

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
