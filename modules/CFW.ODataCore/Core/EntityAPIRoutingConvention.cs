using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace CFW.ODataCore.Core;

[Obsolete("Use minimal api")]
public class EntityAPIRoutingConvention : IControllerModelConvention
{
    private readonly ODataMetadataContainer _container;

    public EntityAPIRoutingConvention(ODataMetadataContainer container)
    {
        _container = container;
    }

    public void Apply(ControllerModel controller)
    {
        var metadata = _container.EntitySetMetadataList
            .FirstOrDefault(x => x.ControllerType == controller.ControllerType);

        if (metadata is null)
            return;

        metadata.ApplyActionModel(controller);
    }
}
