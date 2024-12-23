namespace CFW.ODataCore.Features.EntityQuery;

public class EntityDeleteAttribute<TODataViewModel, TKey>
    : BoundEntityRoutingAttribute
{
    public EntityDeleteAttribute(string name)
        : base(name, ODataMethod.Delete, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityDeleteAttribute
    : BoundEntityRoutingAttribute
{
    public EntityDeleteAttribute(string name)
        : base(name, ODataMethod.Delete)
    {
    }
}
