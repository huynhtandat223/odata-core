namespace CFW.ODataCore.Features.EntityQuery;

public class EntityGetByKeyAttribute<TODataViewModel, TKey>
    : BoundEntityRoutingAttribute
{
    public EntityGetByKeyAttribute(string name)
        : base(name, ODataMethod.GetByKey, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityGetByKeyAttribute
    : BoundEntityRoutingAttribute
{
    public EntityGetByKeyAttribute(string name)
        : base(name, ODataMethod.GetByKey)
    {
    }
}
