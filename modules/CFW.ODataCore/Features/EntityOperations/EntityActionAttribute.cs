namespace CFW.ODataCore.Features.EntityOperations;

public class EntityActionAttribute<TODataViewModel, TKey> : EntityOperationAttribute
{
    public EntityActionAttribute(string name)
        : base(name, EndpointAction.BoundAction, typeof(TODataViewModel), typeof(TKey))
    {
    }
}
