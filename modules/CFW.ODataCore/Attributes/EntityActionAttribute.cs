using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class EntityActionAttribute : BaseRoutingAttribute
{
    public string ActionName { get; init; }

    /// <summary>
    /// BoundEntityType can be target type of multiple entity attribute <see cref="EntityAttribute"/>
    /// If empty, the value will be taken from the first entity name of those attributes.
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// HttpMethod for action.
    /// </summary>
    public ApiMethod HttpMethod { get; set; } = ApiMethod.Post;

    internal Type? TargetType { get; set; }

    internal Type BoundEntityType { get; init; }

    public EntityActionAttribute(string actionName, Type boundEntityType)
    {
        ActionName = actionName;
        BoundEntityType = boundEntityType;
    }
}

public class EntityActionAttribute<TEntity> : EntityActionAttribute
{
    public EntityActionAttribute(string actionName)
        : base(actionName, typeof(TEntity))
    {
    }
}
