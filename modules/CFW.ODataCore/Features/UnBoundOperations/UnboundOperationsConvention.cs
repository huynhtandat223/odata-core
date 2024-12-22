using CFW.ODataCore.Core;
using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;

namespace CFW.ODataCore.Features.UnBoundOperations;

public class UnboundOperationsConvention : IControllerModelConvention
{
    private readonly ODataMetadataContainer _container;

    public UnboundOperationsConvention(ODataMetadataContainer container)
    {
        _container = container;
    }

    public void Apply(ControllerModel controller)
    {
        var metadata = _container.UnBoundOperationMetadataList.FirstOrDefault(x => x.ControllerType == controller.ControllerType);
        if (metadata == null)
            return;

        var routePrefix = _container.RoutePrefix;
        var edmModel = _container.EdmModel;

        var name = metadata.Attribute.Name;

        var edmOperation = edmModel.SchemaElements
            .OfType<IEdmOperation>()
            .Single(x => !x.IsBound && x.Name == name);

        var requestType = metadata.RequestType;
        var oprationType = metadata.Attribute.OperationType;

        var controlerAction = oprationType == OperationType.Action
            ? controller.Actions
                .Single(a => a.ActionName == nameof(UnboundOperationsController<object, object>.ExecuteUnboundAction))
            : controller.Actions
                .Single(a => a.ActionName == nameof(UnboundOperationsController<object, object>.ExecuteUnboundFunction));

        var template = new ODataPathTemplate(new UnboundOperationTemplate(edmOperation));

        var httpMethod = oprationType == OperationType.Action ? HttpMethod.Post.Method : HttpMethod.Get.Method;
        controlerAction.AddSelector(httpMethod, routePrefix, edmModel, template);

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
