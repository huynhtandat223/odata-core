using Microsoft.AspNetCore.OData;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CFW.ODataCore.Core;

public class ODataContainerCollection
{

    public static ODataContainerCollection Instance => new ODataContainerCollection();

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

        var dbEntities = new List<Type>();
        foreach (var container in _containers)
        {
            foreach (var entity in container._entityMetadataList)
            {
                dbEntities.Add(entity.EntityType);
            }
        }
        mvcBuilder.Services.AddSingleton(dbEntities);
        return mvcBuilder.Services;
    }

    public ODataMetadataEntity GetMetadataEntity(TypeInfo controllerType)
    {
        foreach (var container in _containers)
        {
            var metadataEntity = container._entityMetadataList.FirstOrDefault(x => x.ControllerType == controllerType);
            if (metadataEntity != null)
                return metadataEntity;
        }
        throw new InvalidOperationException($"Controller type {controllerType} is not registered.");
    }
}
