using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Features.UnBoundOperations;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class UnboundOperationAttribute : EndpointAttribute
{
    public UnboundOperationAttribute(string name, EndpointAction method)
        : base(name, method)
    {

    }
}
