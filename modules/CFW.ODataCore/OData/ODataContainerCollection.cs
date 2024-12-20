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

    public void Clear()
    {
        _containers.Clear();
    }

    public IMvcBuilder Build(IMvcBuilder mvcBuilder)
    {
        return mvcBuilder.AddOData(options =>
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

    public ODataBoundActionMetadata GetBoundActionMetadataEntity(TypeInfo boundActionControllerType)
    {
        foreach (var container in _containers)
        {
            var boundActionMetadata = container.EntityMetadataList
                .SelectMany(x => x.BoundActionMetadataList)
                .FirstOrDefault(x => x.BoundActionControllerType == boundActionControllerType);

            if (boundActionMetadata != null)
                return boundActionMetadata;
        }
        throw new InvalidOperationException($"Controller type {boundActionControllerType} is not registered.");
    }
}
