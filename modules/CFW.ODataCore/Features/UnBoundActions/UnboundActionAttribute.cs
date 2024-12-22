namespace CFW.ODataCore.Features.UnBoundActions;

[AttributeUsage(AttributeTargets.Class)]
public class UnboundActionAttribute : Attribute
{
    public required string Name { get; set; }

    public string? RoutePrefix { get; set; }
}