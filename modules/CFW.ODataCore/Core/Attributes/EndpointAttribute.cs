namespace CFW.ODataCore.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public abstract class EndpointAttribute : Attribute
{
    public string Name { get; set; }

    public string? RoutePrefix { get; set; }

    public EndpointAction EndpointAction { get; set; }

    public EndpointAttribute(string name, EndpointAction method)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be set", nameof(name));

        Name = name;
        EndpointAction = method;
    }
}

