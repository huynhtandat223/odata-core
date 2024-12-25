using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace CFW.ODataCore.Core.Metadata;

public class UnboundOperationMetadata : EndpointMetadata
{
    public required Type RequestType { get; set; }

    public required Type ResponseType { get; set; }

    public override void ApplyActionModel(ControllerModel controller)
    {
        throw new NotImplementedException();
    }
}
