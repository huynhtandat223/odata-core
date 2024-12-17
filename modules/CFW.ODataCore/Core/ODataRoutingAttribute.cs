namespace CFW.ODataCore.Core;

public class ODataRoutingAttribute : Attribute
{
    public Type? EntityType { get; set; }

    public Type? KeyType { get; set; }

    public string Name { get; set; }

    public string? RouteRefix { get; set; }

    public ODataRoutingAttribute(Type entityType, Type keyType, string name)
    {
        EntityType = entityType;
        KeyType = keyType;
        Name = name;
    }

    public ODataRoutingAttribute(string name)
    {
        Name = name;
    }
}
