using CFW.ODataCore.Models;

namespace CFW.ODataCore.Attributes;

public class UnboundActionAttribute : BaseRoutingAttribute
{
    public string ActionName { get; init; }

    public ApiMethod ActionMethod { get; set; } = ApiMethod.Post;

    public Type? TargetType { get; set; }

    public UnboundActionAttribute(string actionName)
    {
        ActionName = actionName;
    }
}