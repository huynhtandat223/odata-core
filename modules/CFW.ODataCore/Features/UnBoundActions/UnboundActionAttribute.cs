namespace CFW.ODataCore.Features.UnBoundActions;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class UnboundActionAttribute : Attribute
{
    public string Name { get; set; }

    public string? RouteRefix { get; set; }

    public UnboundActionAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be set", nameof(name));

        Name = name;
    }

}