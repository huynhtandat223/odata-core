using CFW.ODataCore.Features.UnBoundActions;
using System.Reflection;

namespace CFW.ODataCore.Features.Core;

public class UnboundActionMetadata
{
    public required Type RequestType { get; set; }

    public required Type ResponseType { get; set; }

    public required Type HandlerType { get; set; }

    public required UnboundActionAttribute UnboundActionAttribute { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; }

    public required TypeInfo ControllerType { get; set; }
}

