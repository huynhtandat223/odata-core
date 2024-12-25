using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;

namespace CFW.ODataCore.Core.Metadata;

public class EntitySetMetadata : EndpointMetadata
{
    public Type? DbSetType { get; set; }

    public required Type ViewModelType { get; set; }

    public required Type KeyType { get; set; }

    public override void ApplyActionModel(ControllerModel controller)
    {
        var entitySet = Container.EdmModel.EntityContainer.FindEntitySet(RoutingAttribute.Name);
        var withoutKeyTemplate = new ODataPathTemplate(new ODataEntitiesTemplate(entitySet, ignoreKeyTemplates: true));
        var routePrefix = Container.RoutePrefix;
        var edmModel = Container.EdmModel;

        var actionModel = controller.Actions.Single(a => a.ActionName == ControllerActionMethodName);

        actionModel.AddSelector(HttpMethod.Post.Method, routePrefix, edmModel, withoutKeyTemplate);
        AddAuthorizationInfo(actionModel);
    }
}
