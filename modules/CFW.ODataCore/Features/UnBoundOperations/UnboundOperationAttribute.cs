using CFW.ODataCore.Core;

namespace CFW.ODataCore.Features.UnBoundOperations;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class UnboundOperationAttribute : Attribute
{
    public string Name { get; set; }

    public string? RoutePrefix { get; set; }

    public OperationType OperationType { get; set; }

    public UnboundOperationAttribute(string name, OperationType operationType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be set", nameof(name));

        Name = name;
        OperationType = operationType;
    }
}

public class UnboundActionAttribute : UnboundOperationAttribute
{
    public UnboundActionAttribute(string name) : base(name, OperationType.Action)
    {

    }
}

public class UnboundFunctionAttribute : UnboundOperationAttribute
{
    public UnboundFunctionAttribute(string name) : base(name, OperationType.Function)
    {
    }
}
