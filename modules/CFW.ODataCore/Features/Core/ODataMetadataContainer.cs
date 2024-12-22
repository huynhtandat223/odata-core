using CFW.ODataCore.Features.UnboundFunctions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.Features.Core;

public class ODataMetadataContainer : ApplicationPart, IApplicationPartTypeProvider
{
    private readonly ODataConventionModelBuilder _modelBuilder;

    private readonly List<ODataMetadataEntity> _entityMetadataList = new List<ODataMetadataEntity>();

    public List<UnboundActionMetadata> UnBoundActions { get; private set; } = new List<UnboundActionMetadata>();

    public List<UnboundFunctionMetadata> UnboundFunctions { get; private set; } = new List<UnboundFunctionMetadata>();

    public IReadOnlyCollection<ODataMetadataEntity> EntityMetadataList => _entityMetadataList.AsReadOnly();

    public string RoutePrefix { get; }

    public override string Name => "ODataMetadataContainer";

    public ODataMetadataContainer(string routePrefix)
    {
        _modelBuilder = new ODataConventionModelBuilder();
        RoutePrefix = routePrefix;
    }

    public void AddEntitySets(string routePrefix, BaseODataMetadataResolver typeResolver, IEnumerable<ODataMetadataEntity> metadataEntities)
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

    internal void AddUnboundActions(List<UnboundActionMetadata> unboudActionMetadataList)
    {
        if (!unboudActionMetadataList.Any())
            return;

        foreach (var unBoundActionMetadata in unboudActionMetadataList)
        {
            var action = _modelBuilder.Action(unBoundActionMetadata.Attribute.Name);
            action.Parameter(unBoundActionMetadata.RequestType, "body");

            if (unBoundActionMetadata.ResponseType == typeof(Result))
                continue;

            if (unBoundActionMetadata.ResponseType.IsCommonGenericCollectionType())
            {
                var elementType = unBoundActionMetadata.ResponseType.GetGenericArguments().Single();
                action.ReturnsCollection(elementType);
            }
            else
            {
                action.Returns(unBoundActionMetadata.ResponseType);
            }
        }
        UnBoundActions = unboudActionMetadataList.ToList();
    }

    internal void AddUnboundFunctions(List<UnboundFunctionMetadata> metadataList)
    {
        if (!metadataList.Any())
            return;

        foreach (var metadata in metadataList)
        {
            var function = _modelBuilder.Function(metadata.RoutingAttribute.Name);
            function.Parameter(metadata.RequestType, "body");

            if (metadata.ResponseType.IsCommonGenericCollectionType())
            {
                var elementType = metadata.ResponseType.GetGenericArguments().Single();
                function.ReturnsCollection(elementType);
            }
            else
            {
                function.Returns(metadata.ResponseType);
            }
        }
        UnboundFunctions = metadataList.ToList();
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
        foreach (var controllerType in _entityMetadataList.SelectMany(x => x.GetAllControllerTypes()))
        {
            yield return controllerType;
        }

        foreach (var unboundAction in UnBoundActions)
        {
            yield return unboundAction.ControllerType;
        }

        foreach (var unboundFunction in UnboundFunctions)
        {
            yield return unboundFunction.ControllerType;
        }
    }
}
