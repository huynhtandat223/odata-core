using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

/// <summary>
/// The class marked with this attribute will be used to create an entity set segment.
/// If entity class: the CRUD operations will be generated base on all.
/// If handler class: the CRUD operations will be generated base on CRUD interfaces.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class EntityAttribute : BaseRoutingAttribute
{
    public Type? TargetType { get; set; }

    public string Name { get; }

    public EntityMethod[] Methods { get; set; } = Enum.GetValues<EntityMethod>().ToArray();

    public EntityAttribute(string name)
    {
        if (name.IsNullOrWhiteSpace())
        {
            throw new ArgumentNullException(nameof(name));
        }

        Name = name;
    }
}