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

    public List<ODataBoundActionMetadata> BoundActionMetadataList { get; set; } = new List<ODataBoundActionMetadata>();

    public List<ODataBoundActionMetadata> BoundFunctionMetadataList { get; set; } = new List<ODataBoundActionMetadata>();

    public IEnumerable<TypeInfo> GetAllControllerTypes()
    {
        yield return ControllerType;
        foreach (var boundActionMetadata in BoundActionMetadataList)
        {
            yield return boundActionMetadata.ControllerType;
        }

        foreach (var boundFunctionMetadata in BoundFunctionMetadataList)
        {
            yield return boundFunctionMetadata.ControllerType;
        }
    }
}
