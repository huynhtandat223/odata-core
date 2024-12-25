using CFW.ODataCore.Core.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Reflection;

namespace CFW.ODataCore.Core.Metadata;

public abstract class EndpointMetadata
{
    /// <summary>
    /// The interface of abstract service handler.
    /// </summary>
    public required Type ServiceHandlerType { get; set; }

    public required Type ServiceImplemenationType { get; set; }

    public required ODataMetadataContainer Container { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required string ControllerActionMethodName { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; } = Array.Empty<Attribute>();

    public required EndpointAttribute RoutingAttribute { get; set; }

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
