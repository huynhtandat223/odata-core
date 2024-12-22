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

    public void AddEntitySets(string routePrefix, BaseODataMetadataResolver typeResolver, IEnumerable<ODataMetadataEntity> oDataTypes)
    {
        foreach (var metadataEntity in oDataTypes)
        {
            var entityType = _modelBuilder.AddEntityType(metadataEntity.ViewModelType);
            _modelBuilder.AddEntitySet(metadataEntity.Name, entityType);

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

            foreach (var operationMetadata in metadataEntity.BoundFunctionMetadataList)
            {
                var operationName = operationMetadata.BoundActionAttribute.Name;
                var operation = _modelBuilder.Function(operationName);

                operation.SetBindingParameter(BindingParameterConfiguration.DefaultBindingParameterName, entityType);
                operation.Parameter(operationMetadata.RequestType, "body");

                if (operationMetadata.ResponseType == typeof(Result))
                    throw new InvalidOperationException("Functions can't use Result type");

                if (operationMetadata.ResponseType.IsCommonGenericCollectionType())
                {
                    var elementType = operationMetadata.ResponseType.GetGenericArguments().Single();
                    operation.ReturnsCollection(elementType);
                }
                else
                {
                    operation.Returns(operationMetadata.ResponseType);
                }
            }


            _entityMetadataList.Add(metadataEntity);
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
