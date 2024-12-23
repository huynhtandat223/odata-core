using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace CFW.ODataCore.Core;

public class EntityRoutingConvention : IControllerModelConvention
{
    private readonly ODataMetadataContainer _container;

    public EntityRoutingConvention(ODataMetadataContainer container)
    {
        _container = container;
    }

    public void Apply(ControllerModel controller)
    {
        var metadata = _container.APIMetadataList
            .FirstOrDefault(x => x.ControllerType == controller.ControllerType);

        if (metadata is null)
            return;

        metadata.ApplyActionModel(controller);
    }
}
