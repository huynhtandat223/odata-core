using CFW.ODataCore.Features.EntityCreate;
using CFW.ODataCore.Features.EntitySets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;
using System.Reflection;

namespace CFW.ODataCore.Core;

public abstract class APIMetadata
{
    public required ODataMethod Method { get; set; }

    public required ODataMetadataContainer Container { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required string ControllerActionMethodName { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; } = Array.Empty<Attribute>();

    public required ODataAPIRoutingAttribute RoutingAttribute { get; set; }

    public abstract void AddDependencies(IServiceCollection services);

    public abstract void ApplyActionModel(ControllerModel controller);

    protected void AddAuthorizationInfo(ActionModel actionModel)
    {
        var authorizeAttr = SetupAttributes.OfType<AuthorizeAttribute>().SingleOrDefault();
        if (authorizeAttr is not null)
        {
            var authorizeFilter = new AuthorizeFilter([authorizeAttr]);
            actionModel.Filters.Add(authorizeFilter);
        }

        var anonymousAttr = SetupAttributes.OfType<AllowAnonymousAttribute>().SingleOrDefault();
        if (anonymousAttr is not null)
        {
            var anonymousFilter = new AllowAnonymousFilter();
            actionModel.Filters.Add(anonymousFilter);
        }
    }
}


public class BoundAPIMetadata : APIMetadata
{
    public Type? DbSetType { get; set; }

    public required Type ViewModelType { get; set; }

    public required Type KeyType { get; set; }

    public IEnumerable<ODataBoundOperationMetadata> BoundOperationMetadataList { get; set; }
        = Array.Empty<ODataBoundOperationMetadata>();

    public override void AddDependencies(IServiceCollection services)
    {
        if (Method == ODataMethod.PostCreate)
        {
            var serviceType = typeof(IEntityCreateHandler<,>).MakeGenericType(ViewModelType, KeyType);
            var implementationType = typeof(EntityCreateDefaultHandler<,>).MakeGenericType(ViewModelType, KeyType);

            services.AddScoped(serviceType, s => ActivatorUtilities.CreateInstance(s, implementationType, this));
        }
    }

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

[Obsolete]
public class ODataMetadataEntity
{
    public required ODataMetadataContainer Container { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required string Name { get; set; }

    public required Type ViewModelType { get; set; }

    public required Type KeyType { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; } = Array.Empty<Attribute>();

    public required ODataEntitySetAttribute DataRoutingAttribute { get; set; }

    public Type? DbSetType { get; set; }

    public IEnumerable<ODataBoundOperationMetadata> BoundOperationMetadataList { get; set; } = new List<ODataBoundOperationMetadata>();

    public IEnumerable<TypeInfo> GetAllControllerTypes()
    {
        yield return ControllerType;
        foreach (var operationMetadata in BoundOperationMetadataList)
        {
            yield return operationMetadata.ControllerType;
        }
    }
}
