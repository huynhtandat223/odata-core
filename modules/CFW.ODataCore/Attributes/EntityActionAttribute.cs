using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class EntityActionAttribute : BaseRoutingAttribute
{
    public string ActionName { get; init; }

    public Type BoundEntityType { get; init; }

    /// <summary>
    /// Because BoundEntityType can be target type multiple entity attribute <see cref="EntityAttribute"/>
    /// If empty, the value will be taken from the first entity name of those attributes.
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// HttpMethod for action.
    /// </summary>
    public ApiMethod ActionMethod { get; set; } = ApiMethod.Post;

    public Type? TargetType { get; set; }

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
