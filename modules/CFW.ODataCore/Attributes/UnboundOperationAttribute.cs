using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

public abstract class UnboundOperationAttribute : ODataRoutingAttribute
{
    public string OperationName { get; init; }

    public OperationType OperationType { get; set; } = OperationType.Action;

    public UnboundOperationAttribute(string operationName, OperationType operationType)
    {
        OperationName = operationName;
        OperationType = operationType;
    }
}
