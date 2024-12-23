namespace CFW.ODataCore.Core;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public abstract class ODataAPIRoutingAttribute : Attribute
{
    public string Name { get; set; }

    public string? RouteRefix { get; set; }

    public ODataMethod Method { get; set; }

    public ODataAPIRoutingAttribute(string name, ODataMethod method)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be set", nameof(name));

        Name = name;
        Method = method;
    }
}

public abstract class BoundEntityRoutingAttribute : ODataAPIRoutingAttribute
{
    public Type? DbSetType { get; set; }

    public Type ViewModelType { get; set; }

    public Type KeyType { set; get; }

    public BoundEntityRoutingAttribute(string name, ODataMethod method, Type viewModelType, Type keyType)
        : base(name, method)
    {
        ViewModelType = viewModelType;
        KeyType = keyType;
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

