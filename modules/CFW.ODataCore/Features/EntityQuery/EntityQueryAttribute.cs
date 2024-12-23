namespace CFW.ODataCore.Features.EntityQuery;

public class EntityQueryAttribute<TODataViewModel, TKey>
    : BoundEntityRoutingAttribute
{
    public EntityQueryAttribute(string name)
        : base(name, ODataMethod.Query, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityQueryAttribute
    : BoundEntityRoutingAttribute
{
    public EntityQueryAttribute(string name)
        : base(name, ODataMethod.Query)
    {
    }
}
