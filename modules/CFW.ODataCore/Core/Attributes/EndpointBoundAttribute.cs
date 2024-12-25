namespace CFW.ODataCore.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public abstract class EndpointBoundAttribute : EndpointAttribute
{
    public Type BoundEntityType { get; set; } = default!;

    public Type BoundKeyType { set; get; } = default!;

    public EndpointBoundAttribute(string name, EndpointAction method, Type viewModelType, Type keyType)
        : base(name, method)
    {
        BoundEntityType = viewModelType;
        BoundKeyType = keyType;
    }

    public EndpointBoundAttribute(string name, EndpointAction method)
        : base(name, method)
    {

    }
}
