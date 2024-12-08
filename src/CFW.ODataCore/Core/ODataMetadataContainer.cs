using CFW.ODataCore.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.Core;

public class ODataMetadataContainer : ApplicationPart, IApplicationPartTypeProvider
{
    private readonly ODataConventionModelBuilder _modelBuilder;

    public readonly List<ODataMetadataEntity> _entityMetadataList = new List<ODataMetadataEntity>();

    public string RoutePrefix { get; }

    public override string Name => "ODataMetadataContainer";

    public ODataMetadataContainer(string routePrefix)
    {
        _modelBuilder = new ODataConventionModelBuilder();
        RoutePrefix = routePrefix;
    }


    public ODataMetadataContainer AddEntitySet(ODataRoutingAttribute routingAttribute)
    {
        var entityType = routingAttribute.EntityType;
        var keyType = routingAttribute.KeyType;

        if (entityType is null || keyType is null)
            throw new InvalidOperationException("EntityType and KeyType must be set");

        var entityTypeConfig = _modelBuilder.AddEntityType(entityType);
        var entitySet = _modelBuilder.AddEntitySet(routingAttribute.Name, entityTypeConfig);

        var controlerType = typeof(EntitySetsController<,>).MakeGenericType([entityType, keyType]).GetTypeInfo();

        _entityMetadataList.Add(new ODataMetadataEntity
        {
            EntityType = entityType,
            Name = routingAttribute.Name,
            Container = this,
            ControllerType = controlerType
        });

        return this;
    }

    private IEdmModel? _edmModel;

    public IEdmModel EdmModel
    {
        get
        {
            if (_edmModel == null)
                _edmModel = _modelBuilder.GetEdmModel();
            return _edmModel;
        }
    }

    // IApplicationPartTypeProvider implementation
    public IEnumerable<TypeInfo> Types => _entityMetadataList.Select(x => x.ControllerType);
}
