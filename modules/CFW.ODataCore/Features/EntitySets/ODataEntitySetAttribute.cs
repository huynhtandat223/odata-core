namespace CFW.ODataCore.Features.EntitySets;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ODataEntitySetAttribute : Attribute
{
    public string Name { get; set; }

    public string? RouteRefix { get; set; }

    public ODataMethod[]? AllowMethods { get; set; }

    public ODataEntitySetAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be set", nameof(name));

        Name = name;
    }
}

public enum ODataMethod
{
    Query = 1,
    GetByKey,
    PostCreate,
    PatchUpdate,
    Delete,
}
