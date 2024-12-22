using CFW.ODataCore.Features.Core;
using CFW.ODataCore.Features.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using System.Reflection;

namespace CFW.ODataCore.Features.BoundOperations;

public class BoundOperationsConvention : IControllerModelConvention
{
    private readonly ODataMetadataContainer _container;

    public BoundOperationsConvention(ODataMetadataContainer container)
    {
        _container = container;
    }

    public void Apply(ControllerModel controller)
    {
        var metadata = _container.EntityMetadataList
            .SelectMany(x => x.BoundOperationMetadataList)
            .FirstOrDefault(x => x.ControllerType == controller.ControllerType);

        if (metadata is null)
            return;

        var entitySet = metadata.Container.EdmModel.EntityContainer.FindEntitySet(metadata.BoundCollectionName);
        var entityFullName = entitySet.EntityType().FullName();
        var routePrefix = metadata.Container.RoutePrefix;
        var edmModel = metadata.Container.EdmModel;
        var boundActionName = metadata.BoundOprationAttribute.Name;

        var edmOpr = edmModel.SchemaElements
            .OfType<IEdmOperation>()
            .Where(x => x.IsBound && x.Parameters.First().Type.FullName() == entityFullName)
            .Single(x => x.Name == boundActionName);

        var requestType = metadata.RequestType;
        var keyType = metadata.KeyType;

        var hasKey = requestType.GetProperties()
            .Any(p => p.GetCustomAttribute<FromRouteAttribute>() is not null && p.PropertyType == keyType);

        var ignoreKeyTemplates = !hasKey;
        var template = new ODataPathTemplate(new BoundOperationTemplate(entitySet, ignoreKeyTemplates, edmOpr));

        var controllerActionMethod = metadata.OperationType == OperationType.Action
            ? controller.Actions
                .Single(a => a.ActionName == nameof(BoundOperationsController<RefODataViewModel, int, object, object>.ExecuteAction))
            : controller.Actions
                .Single(a => a.ActionName == nameof(BoundOperationsController<RefODataViewModel, int, object, object>.ExecuteFunction));

        var httpMethod = metadata.OperationType == OperationType.Action ? HttpMethod.Post.Method : HttpMethod.Get.Method;
        controllerActionMethod.AddSelector(httpMethod, routePrefix, edmModel, template);

        var authAttr = metadata.SetupAttributes.OfType<ODataAuthorizeAttribute>().SingleOrDefault();
        var anonymousAttr = metadata.SetupAttributes.OfType<ODataAllowAnonymousAttribute>().SingleOrDefault();

        if (authAttr is not null)
        {
            var authorizeFilter = new AuthorizeFilter([authAttr]);
            controllerActionMethod.Filters.Add(authorizeFilter);
            return;
        }

        if (anonymousAttr is not null)
        {
            var allowAnonymousFilter = new AllowAnonymousFilter();
            controllerActionMethod.Filters.Add(allowAnonymousFilter);
            return;
        }
    }
}
