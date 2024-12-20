using Microsoft.AspNetCore.OData;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CFW.ODataCore.OData;

public class ODataContainerCollection
{
    static ThreadLocal<ODataContainerCollection> _instance
        = new ThreadLocal<ODataContainerCollection>(() => new ODataContainerCollection());

    public static ODataContainerCollection Instance => _instance.Value!;

    private List<ODataMetadataContainer> _containers = new();

    public ReadOnlyCollection<ODataMetadataContainer> MetadataContainers => _containers.AsReadOnly();

    public ODataMetadataContainer AddOrGetContainer(string routePrefix)
    {
        var container = _containers.FirstOrDefault(x => x.RoutePrefix.CompareIgnoreCase(routePrefix));

        if (container is not null)
            return container;

        container = new ODataMetadataContainer(routePrefix);

        _containers.Add(container);
        return container;
    }

    public IServiceCollection Build(IServiceCollection services)
    {
        var mvcBuilder = services.AddControllers().AddOData(options =>
        {
            options.EnableQueryFeatures();

            foreach (var metadataContainer in Instance.MetadataContainers)
            {
                var model = metadataContainer.Build();

                options.AddRouteComponents(
                    routePrefix: metadataContainer.RoutePrefix
                    , model: metadataContainer.EdmModel);
            }
        }).ConfigureApplicationPartManager(pm =>
        {
            foreach (var metadataContainer in Instance.MetadataContainers)
            {
                pm.ApplicationParts.Add(metadataContainer);
            }
        });
        return mvcBuilder.Services;
    }

    public ODataMetadataEntity GetMetadataEntity(TypeInfo controllerType)
    {
        foreach (var container in _containers)
        {
            var metadataEntity = container.EntityMetadataList.FirstOrDefault(x => x.ControllerType == controllerType);
            if (metadataEntity != null)
                return metadataEntity;
        }
        throw new InvalidOperationException($"Controller type {controllerType} is not registered.");
    }
}
