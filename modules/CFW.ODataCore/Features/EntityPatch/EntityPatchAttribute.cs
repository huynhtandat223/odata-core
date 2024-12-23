namespace CFW.ODataCore.Features.EntityQuery;

public class EntityPatchAttribute<TODataViewModel, TKey>
    : BoundEntityRoutingAttribute
{
    public EntityPatchAttribute(string name)
        : base(name, ODataMethod.PatchUpdate, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityPatchAttribute
    : BoundEntityRoutingAttribute
{
    public EntityPatchAttribute(string name)
        : base(name, ODataMethod.PatchUpdate)
    {
    }
}
