using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityPatchAttribute<TODataViewModel, TKey>
    : EndpointEntityActionAttribute
{
    public EntityPatchAttribute(string name)
        : base(name, EndpointAction.PatchUpdate, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityPatchAttribute
    : EndpointEntityActionAttribute
{
    public EntityPatchAttribute(string name)
        : base(name, EndpointAction.PatchUpdate)
    {
    }
}
