namespace CFW.ODataCore.Attributes;

[Obsolete]
public class EntityFunctionAttribute<TEntity> : BoundOperationAttribute
{
    public EntityFunctionAttribute(string operationName)
        : base(operationName, typeof(TEntity))
    {
    }
}