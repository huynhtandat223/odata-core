using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

public class UnboundActionAttribute : BaseRoutingAttribute
{
    public string ActionName { get; init; }

    public ApiMethod ActionMethod { get; set; } = ApiMethod.Post;

    internal Type? TargetType { get; set; }

    public UnboundActionAttribute(string actionName)
    {
        ActionName = actionName;
    }
}