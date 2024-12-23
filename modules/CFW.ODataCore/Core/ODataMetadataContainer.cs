using CFW.ODataCore.Core.MetadataResolvers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.Core;

public class ODataMetadataContainer : ApplicationPart, IApplicationPartTypeProvider
{
    private readonly ODataConventionModelBuilder _modelBuilder;

    private readonly List<ODataMetadataEntity> _entityMetadataList = new List<ODataMetadataEntity>();

    public List<UnboundOperationMetadata> UnBoundOperationMetadataList { get; private set; } = new List<UnboundOperationMetadata>();

    public IReadOnlyCollection<ODataMetadataEntity> EntityMetadataList => _entityMetadataList.AsReadOnly();

    public IEnumerable<APIMetadata> APIMetadataList { get; private set; } = Array.Empty<APIMetadata>();

    public string RoutePrefix { get; }

    public override string Name => "ODataMetadataContainer";

    public ODataMetadataContainer(string routePrefix)
    {
        _modelBuilder = new ODataConventionModelBuilder();
        RoutePrefix = routePrefix;
    }

    public void AddEntitySets(string routePrefix, BaseODataMetadataResolver typeResolver
        , IEnumerable<APIMetadata> metadataList)
    {
        foreach (var apiMetadataItem in metadataList)
        {
            if (apiMetadataItem is BoundAPIMetadata boundAPIMetadata)
            {
                var entityType = _modelBuilder.AddEntityType(boundAPIMetadata.ViewModelType);
                _modelBuilder.AddEntitySet(boundAPIMetadata.RoutingAttribute.Name, entityType);
                foreach (var boundOperationMetadata in boundAPIMetadata.BoundOperationMetadataList)
                {
                    AddOperation(entityType, boundOperationMetadata);
                }

                continue;
            }

            throw new NotImplementedException();
        }

        APIMetadataList = metadataList;
    }

    [Obsolete]
    public void AddEntitySets(string routePrefix, BaseODataMetadataResolver typeResolver
        , IEnumerable<ODataMetadataEntity> metadataEntities)
    {
        foreach (var metadataEntity in metadataEntities)
        {
            var entityType = _modelBuilder.AddEntityType(metadataEntity.ViewModelType);
            _modelBuilder.AddEntitySet(metadataEntity.Name, entityType);

            foreach (var boundOperationMetadata in metadataEntity.BoundOperationMetadataList)
            {
                AddOperation(entityType, boundOperationMetadata);
            }
            _entityMetadataList.Add(metadataEntity);
        }
    }

    private void AddOperation(EntityTypeConfiguration entityType, ODataBoundOperationMetadata boundOperationMetadata)
    {
        var operationName = boundOperationMetadata.BoundOprationAttribute.Name;

        if (boundOperationMetadata.OperationType == OperationType.Action)
        {
            var operation = _modelBuilder.Action(operationName);

            operation.SetBindingParameter(BindingParameterConfiguration.DefaultBindingParameterName, entityType);
            operation.Parameter(boundOperationMetadata.RequestType, "body");

            if (boundOperationMetadata.ResponseType == typeof(Result))
                return;

            if (boundOperationMetadata.ResponseType.IsCommonGenericCollectionType())
            {
                var elementType = boundOperationMetadata.ResponseType.GetGenericArguments().Single();
                operation.ReturnsCollection(elementType);
            }
            else
            {
                operation.Returns(boundOperationMetadata.ResponseType);
            }
        }
        else
        {
            var operation = _modelBuilder.Function(operationName);

            operation.SetBindingParameter(BindingParameterConfiguration.DefaultBindingParameterName, entityType);
            operation.Parameter(boundOperationMetadata.RequestType, "body");

            if (boundOperationMetadata.ResponseType == typeof(Result))
                throw new InvalidOperationException("Functions can't use Result type");

            if (boundOperationMetadata.ResponseType.IsCommonGenericCollectionType())
            {
                var elementType = boundOperationMetadata.ResponseType.GetGenericArguments().Single();
                operation.ReturnsCollection(elementType);
            }
            else
            {
                operation.Returns(boundOperationMetadata.ResponseType);
            }
        }
    }

    internal void AddUnboundOperations(List<UnboundOperationMetadata> unboundOperations)
    {
        if (!unboundOperations.Any())
            return;

        foreach (var operation in unboundOperations)
        {
            if (operation.Attribute.OperationType == OperationType.Action)
            {
                var action = _modelBuilder.Action(operation.Attribute.Name);
                action.Parameter(operation.RequestType, "body");
                if (operation.ResponseType == typeof(Result))
                    continue;
                if (operation.ResponseType.IsCommonGenericCollectionType())
                {
                    var elementType = operation.ResponseType.GetGenericArguments().Single();
                    action.ReturnsCollection(elementType);
                }
                else
                {
                    action.Returns(operation.ResponseType);
                }
            }
            else
            {
                var function = _modelBuilder.Function(operation.Attribute.Name);
                function.Parameter(operation.RequestType, "body");
                if (operation.ResponseType == typeof(Result))
                    continue;
                if (operation.ResponseType.IsCommonGenericCollectionType())
                {
                    var elementType = operation.ResponseType.GetGenericArguments().Single();
                    function.ReturnsCollection(elementType);
                }
                else
                {
                    function.Returns(operation.ResponseType);
                }
            }
        }
        UnBoundOperationMetadataList = unboundOperations.ToList();
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
    public IEnumerable<TypeInfo> Types => GetAllController();

    private IEnumerable<TypeInfo> GetAllController()
    {
        foreach (var apiMetadata in APIMetadataList)
        {
            yield return apiMetadata.ControllerType;
        }
    }
}
