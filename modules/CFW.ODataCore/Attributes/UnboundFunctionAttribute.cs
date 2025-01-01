using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

public class UnboundFunctionAttribute : UnboundOperationAttribute
{
    public UnboundFunctionAttribute(string operationName) : base(operationName, OperationType.Function)
    {
    }
}