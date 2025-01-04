using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

public abstract class BoundOperationAttribute : BaseRoutingAttribute
{
    public string OperationName { get; init; }

    public Type EntityType { get; init; }

    public OperationType OperationType { get; set; } = OperationType.Action;

    public BoundOperationAttribute(string operationName, Type entityType, OperationType operationType)
    {
        OperationName = operationName;
        EntityType = entityType;
        OperationType = operationType;
    }
}
