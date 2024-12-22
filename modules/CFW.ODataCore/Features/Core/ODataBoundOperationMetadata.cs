using CFW.ODataCore.Features.BoundOperations;
using System.Reflection;

namespace CFW.ODataCore.Features.Core;

public class ODataBoundOperationMetadata
{
    public required Type RequestType { get; set; }

    public required Type ResponseType { get; set; }

    public required Type HandlerType { get; set; }

    public required BoundOperationAttribute BoundOprationAttribute { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required ODataMetadataContainer Container { get; set; }

    public required string BoundCollectionName { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; } = Array.Empty<Attribute>();

    public required Type KeyType { get; set; }

    public required OperationType OperationType { get; set; }
}

public enum OperationType
{
    Action,
    Function
}
