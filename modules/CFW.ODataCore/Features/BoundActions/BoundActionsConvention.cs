using CFW.ODataCore.OData;
using CFW.ODataCore.OData.Templates;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;

namespace CFW.ODataCore.Features.BoundActions;

public class BoundActionsConvention : Attribute, IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (!controller.ControllerType.IsGenericType || controller.ControllerType.GetGenericTypeDefinition() != typeof(BoundActionsController<,,,>))
            throw new InvalidOperationException("Controller must be a BoundActionsController<TViewModel, Key, Request, Response>.");

        var boundActionMetadata = ODataContainerCollection.Instance.GetBoundActionMetadataEntity(controller.ControllerType);
        var entitySet = boundActionMetadata.Container.EdmModel.EntityContainer.FindEntitySet(boundActionMetadata.BoundCollectionName);
        var entityFullName = entitySet.EntityType().FullName();
        var routePrefix = boundActionMetadata.Container.RoutePrefix;
        var edmModel = boundActionMetadata.Container.EdmModel;
        var boundActionName = boundActionMetadata.BoundActionAttribute.Name;

        var edmAction = edmModel.SchemaElements
            .OfType<IEdmAction>()
            .Where(x => x.Parameters.First().Type.FullName() == entityFullName)
            .Single(x => x.Name == boundActionName);

        var requestType = boundActionMetadata.RequestType;
        var odataViewModelType = typeof(IODataViewModel<>).MakeGenericType(boundActionMetadata.KeyType);
        var hasKey = requestType.GetInterfaces().Contains(odataViewModelType);

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
