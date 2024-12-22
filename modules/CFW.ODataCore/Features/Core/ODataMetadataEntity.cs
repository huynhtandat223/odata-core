using CFW.ODataCore.Features.EntitySets;
using System.Reflection;

namespace CFW.ODataCore.Features.Core;

public class ODataMetadataEntity
{
    public required ODataMetadataContainer Container { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required string Name { get; set; }

    public required Type ViewModelType { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; } = Array.Empty<Attribute>();

    public required ODataEntitySetAttribute DataRoutingAttribute { get; set; }

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
