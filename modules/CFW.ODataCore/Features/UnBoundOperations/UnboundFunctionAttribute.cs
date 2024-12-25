namespace CFW.ODataCore.Features.UnBoundOperations;

public class UnboundFunctionAttribute : UnboundOperationAttribute
{
    public UnboundFunctionAttribute(string name) : base(name, EndpointAction.UnboundFunction)
    {
    }
}
