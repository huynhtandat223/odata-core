namespace CFW.ODataCore.Features.EntityCreate;

public class EntityCreateAttribute<TODataViewModel, TKey>
    : BoundEntityRoutingAttribute
    where TODataViewModel : IODataViewModel<TKey>

{
    public EntityCreateAttribute(string name)
        : base(name, ODataMethod.PostCreate, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityCreateAttribute
    : BoundEntityRoutingAttribute
{
    public EntityCreateAttribute(string name)
        : base(name, ODataMethod.PostCreate)
    {
    }
}