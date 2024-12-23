using CFW.ODataCore.Features.EntitySets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Reflection;

namespace CFW.ODataCore.Core;

public class APIMetadata
{
    public required ODataMetadataContainer Container { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; } = Array.Empty<Attribute>();

    public required ODataAPIRoutingAttribute RoutingAttribute { get; set; }
}


public class BoundAPIMetadata : APIMetadata
{
    public Type? DbSetType { get; set; }

    public required Type ViewModelType { get; set; }

    public required Type KeyType { get; set; }

    public IEnumerable<ODataBoundOperationMetadata> BoundOperationMetadataList { get; set; }
        = Array.Empty<ODataBoundOperationMetadata>();

    public void AddAuthorizationInfo(ActionModel actionModel)
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
