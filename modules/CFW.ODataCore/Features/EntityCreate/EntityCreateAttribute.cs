using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Features.EntityCreate;

public class EntityCreateAttribute<TODataViewModel, TKey>
    : EndpointEntityActionAttribute
    where TODataViewModel : IODataViewModel<TKey>

{
    public EntityCreateAttribute(string name)
        : base(name, EndpointAction.PostCreate, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityCreateAttribute
    : EndpointEntityActionAttribute
{
    public EntityCreateAttribute(string name)
        : base(name, EndpointAction.PostCreate)
    {
    }
}