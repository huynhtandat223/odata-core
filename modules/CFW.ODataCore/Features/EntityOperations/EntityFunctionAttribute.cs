namespace CFW.ODataCore.Features.EntityOperations;

public class EntityFunctionAttribute<TODataViewModel, TKey> : EntityOperationAttribute
{
    public EntityFunctionAttribute(string name)
        : base(name, EndpointAction.BoundFunction, typeof(TODataViewModel), typeof(TKey))
    {
    }
}
