namespace CFW.ODataCore.Core.Attributes;

public class EndpointEntityActionAttribute : EndpointBoundAttribute
{
    public Type? DbSetType { get; set; }

    public EndpointEntityActionAttribute(string name, EndpointAction method, Type viewModelType, Type keyType)
        : base(name, method)
    {
        BoundEntityType = viewModelType;
        BoundKeyType = keyType;
    }

    public EndpointEntityActionAttribute(string name, EndpointAction method)
        : base(name, method)
    {

    }
}
