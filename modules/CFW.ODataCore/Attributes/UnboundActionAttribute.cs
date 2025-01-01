using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

public class UnboundActionAttribute : UnboundOperationAttribute
{
    public UnboundActionAttribute(string operationName) : base(operationName, OperationType.Action)
    {
    }
}
