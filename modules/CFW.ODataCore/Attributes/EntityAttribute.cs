using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

/// <summary>
/// The class marked with this attribute will be used to create an entity set segment.
/// If entity class: the CRUD operations will be generated base on all.
/// If handler class: the CRUD operations will be generated base on CRUD interfaces.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class EntityAttribute : ODataRoutingAttribute
{
    public string Name { get; }

    /// <summary>
    /// Only effective for viewModel class. Handler class methods resolved by CRUD interfaces.
    /// </summary>
    public ODataHttpMethod[] Methods { get; set; } = Array.Empty<ODataHttpMethod>();

    public EntityAttribute(string name)
    {
        Name = name;
    }

    public EntityAttribute(string name, params string[] odataHttpMethods)
    {
        Name = name;
    }
}
