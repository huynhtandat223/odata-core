namespace CFW.ODataCore.Attributes;

[Obsolete]
public abstract class BoundOperationAttribute : BaseRoutingAttribute
{
    public string Name { get; init; }

    public Type EntityType { get; init; }

    public HttpMethod HttpMethod { get; set; } = HttpMethod.Post;

    public BoundOperationAttribute(string operationName, Type entityType)
    {
        Name = operationName;
        EntityType = entityType;
    }
}
