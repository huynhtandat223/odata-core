using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

public class EntityActionAttribute<TEntity> : BoundOperationAttribute
{
    public EntityActionAttribute(string operationName)
        : base(operationName, typeof(TEntity), OperationType.Action)
    {
    }
}
