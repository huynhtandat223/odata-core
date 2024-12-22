using CFW.ODataCore.Features.BoundActions;
using System.Reflection;

namespace CFW.ODataCore.Features.Core;

public class ODataBoundActionMetadata
{
    public required Type RequestType { get; set; }

    public required Type ResponseType { get; set; }

    public required Type HandlerType { get; set; }

    public required BoundOperationAttribute BoundActionAttribute { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required ODataMetadataContainer Container { get; set; }

    public required string BoundCollectionName { get; set; }

    public required Attribute[] SetupAttributes { get; set; } = Array.Empty<Attribute>();

    public required Type KeyType { get; set; }
}
