namespace CFW.ODataCore.Features.UnBoundOperations;

public class UnboundActionAttribute : UnboundOperationAttribute
{
    public UnboundActionAttribute(string name) : base(name, EndpointAction.UnboundAction)
    {

    }
}
