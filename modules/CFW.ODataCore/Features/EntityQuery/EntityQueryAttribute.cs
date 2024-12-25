using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityQueryAttribute<TODataViewModel, TKey>
    : EndpointEntityActionAttribute
{
    public EntityQueryAttribute(string name)
        : base(name, EndpointAction.Query, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityQueryAttribute
    : EndpointEntityActionAttribute
{
    public EntityQueryAttribute(string name)
        : base(name, EndpointAction.Query)
    {
    }
}
