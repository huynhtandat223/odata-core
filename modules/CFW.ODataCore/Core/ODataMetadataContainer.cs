using CFW.ODataCore.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.Core;

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

    internal void AddEntitySets(List<Type> odataTypes)
    {
        foreach (var odataType in odataTypes)
        {
            var interfaces = odataType.GetInterfaces();
            if (interfaces.Length == 0)
                throw new InvalidOperationException("Not found any interface");

            var routingAttribute = odataType.GetCustomAttribute<ODataRoutingAttribute>()!;
            var authorizeInfo = odataType.GetCustomAttribute<AuthorizeAttribute>();
            var anonymousInfo = odataType.GetCustomAttribute<AllowAnonymousAttribute>();

            var odataViewModelType = interfaces
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IODataViewModel<>));
            Type? entityType = null;
            Type? keyType = null;

            if (odataViewModelType is not null)
            {
                entityType = odataType;
                keyType = odataViewModelType.GetGenericArguments().Single();
            }

            if (entityType is null || keyType is null)
                throw new InvalidOperationException("EntityType and KeyType must be set");

            var controlerType = typeof(EntitySetsController<,>).MakeGenericType([entityType, keyType]).GetTypeInfo();

            _modelBuilder.AddEntitySet(routingAttribute.Name, _modelBuilder.AddEntityType(entityType));

            _entityMetadataList.Add(new ODataMetadataEntity
            {
                EntityType = entityType,
                Name = routingAttribute.Name,
                Container = this,
                ControllerType = controlerType,
                AuthorizeAttribute = authorizeInfo,
                AllowAnonymousAttribute = anonymousInfo
            });
        }
    }

    private IEdmModel? _edmModel;

    public IEdmModel Build()
    {
        _edmModel = _modelBuilder.GetEdmModel();
        _edmModel.MarkAsImmutable();
        return _edmModel;
    }

    public IEdmModel EdmModel => _edmModel ?? throw new InvalidOperationException();


    // IApplicationPartTypeProvider implementation
    public IEnumerable<TypeInfo> Types => _entityMetadataList.Select(x => x.ControllerType);
}
