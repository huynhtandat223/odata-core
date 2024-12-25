using CFW.ODataCore.Core.Attributes;
using CFW.ODataCore.Core.Metadata;
using CFW.ODataCore.Features.EntityCreate;
using CFW.ODataCore.Features.EntityQuery;
using System.Reflection;

namespace CFW.ODataCore.Core.MetadataResolvers;

public class EntitySetMetadataFactory
{

    public EntitySetMetadata? CreateEntitySetMetadata(Type applyType, EndpointEntityActionAttribute routingAttribute
        , ODataMetadataContainer container)
    {
        var viewModelType = routingAttribute.BoundEntityType;
        var keyType = routingAttribute.BoundKeyType;

        if (viewModelType is null)
        {
            var odataViewModelInterface = applyType.GetInterfaces()
                .Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IODataViewModel<>));

            viewModelType = applyType;
            keyType = odataViewModelInterface.GetGenericArguments().Single();
        }

        var entitySetMethodMapping = new Dictionary<EndpointAction, (string ActionName, Type ControllerType
            , Type ServiceHandlerType, Type ServiceImplemenationType)>
        {
            [EndpointAction.PostCreate] = (nameof(EntityCreateController<RefODataViewModel, int>.Post)
                , typeof(EntityCreateController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityCreateHandler<,>), typeof(EntityCreateDefaultHandler<,>)),

            [EndpointAction.Query] = (nameof(EntityQueryController<RefODataViewModel, int>.Query)
                , typeof(EntityQueryController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityQueryHandler<,>), typeof(EntityQueryDefaultHandler<,>)),

            [EndpointAction.GetByKey] = (nameof(EntityGetByKeyController<RefODataViewModel, int>.GetByKey)
                , typeof(EntityGetByKeyController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityGetByKeyHandler<,>), typeof(EntityGetByKeyDefaultHandler<,>)),

            [EndpointAction.PatchUpdate] = (nameof(EntityPatchController<RefODataViewModel, int>.Patch)
                , typeof(EntityPatchController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityGetByKeyHandler<,>), typeof(EntityGetByKeyDefaultHandler<,>)),

            [EndpointAction.Delete] = (nameof(EntityDeleteController<RefODataViewModel, int>.Delete)
                , typeof(EntityDeleteController<,>).MakeGenericType(viewModelType, keyType)
                , typeof(IEntityDeleteHandler<,>), typeof(EntityDeleteDefaultHandler<,>)),
        };

        if (entitySetMethodMapping.TryGetValue(routingAttribute.EndpointAction, out var mappingInfo))
        {
            var metadata = new EntitySetMetadata
            {
                ServiceHandlerType = mappingInfo.ServiceHandlerType,
                ServiceImplemenationType = mappingInfo.ServiceImplemenationType,
                DbSetType = routingAttribute.DbSetType,
                RoutingAttribute = routingAttribute,
                Container = container,
                ViewModelType = viewModelType,
                KeyType = keyType,
                ControllerActionMethodName = mappingInfo.ActionName,
                ControllerType = mappingInfo.ControllerType.GetTypeInfo(),
                SetupAttributes = applyType.GetCustomAttributes(),
            };
            return metadata;
        }

        throw new InvalidOperationException($"Invalid ODataMethod {routingAttribute.EndpointAction} for {applyType}");
    }
}
