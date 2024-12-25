using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityGetByKeyAttribute<TODataViewModel, TKey>
    : EndpointEntityActionAttribute
{
    public EntityGetByKeyAttribute(string name)
        : base(name, EndpointAction.GetByKey, typeof(TODataViewModel), typeof(TKey))
    {
    }
}

public class EntityGetByKeyAttribute
    : EndpointEntityActionAttribute
{
    public EntityGetByKeyAttribute(string name)
        : base(name, EndpointAction.GetByKey)
    {
    }
}
