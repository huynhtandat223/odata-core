using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityDeleteAttribute<TODataViewModel, TKey>
    : EndpointEntityActionAttribute
{
    public EntityDeleteAttribute(string name)
        : base(name, EndpointAction.Delete, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityDeleteAttribute
    : EndpointEntityActionAttribute
{
    public EntityDeleteAttribute(string name)
        : base(name, EndpointAction.Delete)
    {
    }
}
