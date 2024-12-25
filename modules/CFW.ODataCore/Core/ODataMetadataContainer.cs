using CFW.ODataCore.Core.Metadata;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.Core;

public class ODataMetadataContainer : ApplicationPart, IApplicationPartTypeProvider
{

    private readonly ODataConventionModelBuilder _modelBuilder;

    public List<UnboundOperationMetadata> UnBoundOperationMetadataList { get; private set; }
        = new List<UnboundOperationMetadata>();

    public List<BoundOperationMetadata> BoundOperationMetadataList { get; set; }
        = new List<BoundOperationMetadata>();

    public List<EntitySetMetadata> EntitySetMetadataList { get; private set; }
        = new List<EntitySetMetadata>();

    public string RoutePrefix { get; }

    public override string Name => "ODataMetadataContainer";

    public ODataMetadataContainer(string routePrefix)
    {
        _modelBuilder = new ODataConventionModelBuilder();
        RoutePrefix = routePrefix;
    }

    private void AddBoundOperations(EntityTypeConfiguration entityType, IEnumerable<BoundOperationMetadata> boundOperationMetadataList)
    {
        foreach (var boundOperationMetadata in boundOperationMetadataList)
        {
            var operationName = boundOperationMetadata.RoutingAttribute.Name;
            if (boundOperationMetadata.RoutingAttribute.EndpointAction == EndpointAction.BoundAction)
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
    }

    internal void AddUnboundOperations()
    {
        if (!UnBoundOperationMetadataList.Any())
            return;

        foreach (var unboundOperation in UnBoundOperationMetadataList)
        {
            var operationName = unboundOperation.RoutingAttribute.Name;
            if (unboundOperation.RoutingAttribute.EndpointAction == EndpointAction.UnboundAction)
            {
                var action = _modelBuilder.Action(operationName);
                action.Parameter(unboundOperation.RequestType, "body");
                if (unboundOperation.ResponseType == typeof(Result))
                    continue;
                if (unboundOperation.ResponseType.IsCommonGenericCollectionType())
                {
                    var elementType = unboundOperation.ResponseType.GetGenericArguments().Single();
                    action.ReturnsCollection(elementType);
                }
                else
                {
                    action.Returns(unboundOperation.ResponseType);
                }

                continue;
            }

            if (unboundOperation.RoutingAttribute.EndpointAction == EndpointAction.UnboundFunction)
            {
                var function = _modelBuilder.Function(operationName);
                function.Parameter(unboundOperation.RequestType, "body");
                if (unboundOperation.ResponseType == typeof(Result))
                    continue;
                if (unboundOperation.ResponseType.IsCommonGenericCollectionType())
                {
                    var elementType = unboundOperation.ResponseType.GetGenericArguments().Single();
                    function.ReturnsCollection(elementType);
                }
                else
                {
                    function.Returns(unboundOperation.ResponseType);
                }

                continue;
            }

            throw new InvalidOperationException($"Unknown unbound operation type: {unboundOperation.RoutingAttribute.EndpointAction}");
        }
    }

    private IEdmModel? _edmModel;

    public IEdmModel Build()
    {
        if (_edmModel is not null)
            return _edmModel;

        var entitySetMetadataGroup = EntitySetMetadataList
            .GroupBy(e => new { e.ViewModelType, e.KeyType, e.RoutingAttribute.Name });

        foreach (var entitySetGroup in entitySetMetadataGroup)
        {
            var metadata = entitySetGroup.First();
            var entityType = _modelBuilder.AddEntityType(metadata.ViewModelType);
            _modelBuilder.AddEntitySet(metadata.RoutingAttribute.Name, entityType);

            var boundOperations = BoundOperationMetadataList.Where(b => b.BoundEntitySetMetadata == metadata);
            if (boundOperations.Any())
            {
                AddBoundOperations(entityType, boundOperations);
            }
        }

        AddUnboundOperations();

        _edmModel = _modelBuilder.GetEdmModel();
        _edmModel.MarkAsImmutable();
        return _edmModel;
    }

    public IEdmModel EdmModel => _edmModel ?? throw new InvalidOperationException();


    // IApplicationPartTypeProvider implementation
    public IEnumerable<TypeInfo> Types => GetAllController();

    private IEnumerable<TypeInfo> GetAllController()
    {
        foreach (var apiMetadata in EntitySetMetadataList)
        {
            yield return apiMetadata.ControllerType;
        }

        foreach (var boundOperation in BoundOperationMetadataList)
        {
            yield return boundOperation.ControllerType;
        }

        foreach (var unboundOperation in UnBoundOperationMetadataList)
        {
            yield return unboundOperation.ControllerType;
        }
    }
}
