using CFW.ODataCore.Features.UnBoundOperations;
using System.Reflection;

namespace CFW.ODataCore.Core;

public class UnboundOperationMetadata
{
    public required Type RequestType { get; set; }

    public required Type ResponseType { get; set; }

    public required Type HandlerType { get; set; }

    public required UnboundOperationAttribute Attribute { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; }

    public required TypeInfo ControllerType { get; set; }
}
