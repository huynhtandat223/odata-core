using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

public class EntityV2Attribute : BaseRoutingAttribute
{
    public Type? TargetType { get; set; }

    public string Name { get; }

    public EntityMethod[] Methods { get; set; } = Enum.GetValues<EntityMethod>().ToArray();

    public EntityV2Attribute(string name)
    {
        if (name.IsNullOrWhiteSpace())
        {
            throw new ArgumentNullException(nameof(name));
        }

        Name = name;
    }
}

/// <summary>
/// The class marked with this attribute will be used to create an entity set segment.
/// If entity class: the CRUD operations will be generated base on all.
/// If handler class: the CRUD operations will be generated base on CRUD interfaces.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class EntityAttribute : BaseRoutingAttribute
{
    public string Name { get; }

    [Obsolete($"Use {nameof(ConfigurationType)} with type {nameof(DefaultEfCoreConfiguration<object>)} instead")]
    public Type? DbType { get; set; }

    /// <summary>
    /// Only effective for viewModel class. Handler class methods resolved by CRUD interfaces.
    /// </summary>
    public EntityMethod[] Methods { get; set; } = Array.Empty<EntityMethod>();

    /// <summary>
    /// Advanced configuration for entity, the type must implement <see cref="EntityConfiguration{TEntity}"/>.
    /// </summary>
    public Type? ConfigurationType { get; set; }

    public EntityAttribute(string name)
    {
        Name = name;
    }
}

[Obsolete("End investigation")]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ConfigurableEntityAttribute : BaseRoutingAttribute
{
    public string Name { get; }

    /// <summary>
    /// Advanced configuration for entity, the type must implement <see cref="EntityConfiguration{TEntity}"/>.
    /// </summary>
    public Type? ConfigurationType { get; set; }

    public EntityMethod[] Methods { get; set; } = Enum.GetValues<EntityMethod>().ToArray();

    public ConfigurableEntityAttribute(string name)
    {
        Name = name;
    }
}
