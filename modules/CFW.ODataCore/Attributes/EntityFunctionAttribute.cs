using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

public class EntityFunctionAttribute<TEntity> : BoundOperationAttribute
{
    public EntityFunctionAttribute(string operationName)
        : base(operationName, typeof(TEntity), OperationType.Function)
    {
    }
}