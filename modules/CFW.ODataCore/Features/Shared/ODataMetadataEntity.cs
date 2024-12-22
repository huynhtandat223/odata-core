using CFW.ODataCore.Features.BoundActions;
using System.Reflection;

namespace CFW.ODataCore.Features.Shared;

public class ODataMetadataEntity
{
    public required ODataMetadataContainer Container { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required string Name { get; set; }

    public required Type ViewModelType { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; } = Array.Empty<Attribute>();

    public required ODataRoutingAttribute DataRoutingAttribute { get; set; }

    public List<ODataBoundActionMetadata> BoundActionMetadataList { get; set; } = new List<ODataBoundActionMetadata>();

    public IEnumerable<TypeInfo> GetAllControllerTypes()
    {
        yield return ControllerType;
        foreach (var boundActionMetadata in BoundActionMetadataList)
        {
            yield return boundActionMetadata.BoundActionControllerType;
        }
    }
}

public class ODataBoundActionMetadata
{
    public required Type RequestType { get; set; }

    public required Type ResponseType { get; set; }

    public required Type HandlerType { get; set; }

    public required BoundActionAttribute BoundActionAttribute { get; set; }

    public required TypeInfo BoundActionControllerType { get; set; }

    public required ODataMetadataContainer Container { get; set; }

    public required string BoundCollectionName { get; set; }

    public required Attribute[] SetupAttributes { get; set; } = Array.Empty<Attribute>();

    public required Type KeyType { get; set; }
}
