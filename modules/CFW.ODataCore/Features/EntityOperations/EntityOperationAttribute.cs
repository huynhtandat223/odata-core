using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Features.EntityOperations;

public abstract class EntityOperationAttribute : EndpointBoundAttribute
{
    protected EntityOperationAttribute(string name, EndpointAction method, Type viewModelType, Type keyType)
        : base(name, method, viewModelType, keyType)
    {
    }
}
