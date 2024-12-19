namespace CFW.ODataCore.Core;

public class ODataRoutingAttribute : Attribute
{
    public Type? EntityType { get; set; }

    public Type? KeyType { get; set; }

    public string Name { get; set; }

    public string? RouteRefix { get; set; }

    public ODataRoutingAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be set", nameof(name));

        Name = name;
    }
}
