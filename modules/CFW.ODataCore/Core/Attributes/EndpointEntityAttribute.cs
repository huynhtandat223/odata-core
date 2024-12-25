namespace CFW.ODataCore.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class EndpointEntityAttribute : EndpointBoundAttribute
{
    public EndpointAction[] AllowMethods { get; set; }
        = [EndpointAction.PostCreate, EndpointAction.Delete, EndpointAction.Query, EndpointAction.GetByKey];

    public EndpointEntityAttribute(string name) : base(name, EndpointAction.CRUD)
    {
    }
}
