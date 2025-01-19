namespace CFW.ODataCore.Attributes;

/// <summary>
/// Marker interface.
/// </summary>
public class EntityHandlerAttribute<TEntity> : EntityHandlerAttribute
{
    public EntityHandlerAttribute() : base(typeof(TEntity))
    {
    }
}

public class EntityHandlerAttribute : BaseRoutingAttribute
{
    public string? Name { get; }

    public Type EntityType { get; }

    /// <summary>
    /// Implementation type
    /// </summary>
    internal Type? TargetType { get; set; }

    internal Type[] InterfaceTypes { get; set; } = Array.Empty<Type>();

    public EntityHandlerAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}


