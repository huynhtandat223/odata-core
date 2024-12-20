using CFW.ODataCore.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.OData;

public class ODataMetadataContainer : ApplicationPart, IApplicationPartTypeProvider
{
    private readonly ODataConventionModelBuilder _modelBuilder;

    private readonly List<ODataMetadataEntity> _entityMetadataList = new List<ODataMetadataEntity>();

    public IReadOnlyCollection<ODataMetadataEntity> EntityMetadataList => _entityMetadataList.AsReadOnly();

    public string RoutePrefix { get; }

    public override string Name => "ODataMetadataContainer";

    public ODataMetadataContainer(string routePrefix)
    {
        _modelBuilder = new ODataConventionModelBuilder();
        RoutePrefix = routePrefix;
    }

    public void AddEntitySets(string routePrefix, BaseODataTypeResolver typeResolver)
    {
        var oDataTypes = typeResolver.GetODataTypes(routePrefix);
        foreach (var oDataType in oDataTypes)
        {
            var viewModelType = oDataType.EntityType;
            var routingAttribute = oDataType.RoutingAttribute;
            var keyType = oDataType.KeyType;
            var controlerType = typeof(EntitySetsController<,>).MakeGenericType([viewModelType, keyType]).GetTypeInfo();

            var entityType = _modelBuilder.AddEntityType(viewModelType);
            _modelBuilder.AddEntitySet(routingAttribute.Name, entityType);

            var metadataEntity = new ODataMetadataEntity
            {
                ViewModelType = viewModelType,
                Name = routingAttribute.Name,
                Container = this,
                ControllerType = controlerType,
                SetupAttributes = viewModelType.GetCustomAttributes().ToArray(),
            };

            metadataEntity.BoundActionMetadataList = typeResolver
                .GetBoundActionMetadataList(viewModelType, keyType, this, routingAttribute.Name)
                .ToList();
            foreach (var boundActionMetadata in metadataEntity.BoundActionMetadataList)
            {
                var actionName = boundActionMetadata.BoundActionAttribute.Name;
                var action = _modelBuilder.Action(actionName);

                action.SetBindingParameter(BindingParameterConfiguration.DefaultBindingParameterName, entityType);
                action.Parameter(boundActionMetadata.RequestType, "body");

                if (boundActionMetadata.ResponseType == typeof(Result))
                    continue;

                if (boundActionMetadata.ResponseType.IsCommonGenericCollectionType())
                {
                    var elementType = boundActionMetadata.ResponseType.GetGenericArguments().Single();
                    action.ReturnsCollection(elementType);
                }
                else
                {
                    action.Returns(boundActionMetadata.ResponseType);
                }
            }

            _entityMetadataList.Add(metadataEntity);
        }
    }

    private IEdmModel? _edmModel;

    public IEdmModel Build()
    {
        if (_edmModel is not null)
            return _edmModel;

        _edmModel = _modelBuilder.GetEdmModel();
        _edmModel.MarkAsImmutable();
        return _edmModel;
    }

    public IEdmModel EdmModel => _edmModel ?? throw new InvalidOperationException();


    // IApplicationPartTypeProvider implementation
    public IEnumerable<TypeInfo> Types => _entityMetadataList
        .SelectMany(x => x.GetAllControllerTypes());
}
