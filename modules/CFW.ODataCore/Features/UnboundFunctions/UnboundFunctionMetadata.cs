using System.Reflection;

namespace CFW.ODataCore.Features.UnboundFunctions;

[AttributeUsage(AttributeTargets.Class)]
public class UnboundFunctionAttribute : Attribute
{
    public string? RoutePrefix { get; set; }

    public string Name { get; set; }

    public UnboundFunctionAttribute(string name)
    {
        if (name.IsNullOrWhiteSpace())
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

        Name = name;
    }
}

public class UnboundFunctionMetadata
{
    public required Type RequestType { get; set; }

    public required Type ResponseType { get; set; }

    public required Type HandlerType { get; set; }

    public required UnboundFunctionAttribute RoutingAttribute { get; set; }

    public required IEnumerable<Attribute> SetupAttributes { get; set; }

    public required TypeInfo ControllerType { get; set; }
}
