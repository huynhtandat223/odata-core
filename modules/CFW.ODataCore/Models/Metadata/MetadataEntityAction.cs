using CFW.ODataCore.Intefaces;
using CFW.ODataCore.RouteMappers.Actions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CFW.ODataCore.Models.Metadata;

public class MetadataEntityAction : MetadataAction
{
    public required Type BoundEntityType { get; init; }

    public required string? EntityName { get; init; }

    internal void AddServices(IServiceCollection services)
    {
        ResolveRequestResponseTypes();

        //register operation services
        var interfaceType = HasResponseData
            ? typeof(IOperationHandler<,>)
                .MakeGenericType(RequestType!, ResponseType!) :
                typeof(IOperationHandler<>).MakeGenericType(RequestType!);
        var implementationType = TargetType;

        services.TryAddScoped(interfaceType, implementationType);

        //register operation routes
        Type? routeMapperType = null;
        if (HasKey && HasResponseData)
            routeMapperType = typeof(DefaultEntityActionHasResponseRequestHandler<,,>)
                .MakeGenericType(RequestType!, KeyProperty!.PropertyType, ResponseType!);

        if (!HasKey && HasResponseData)
            routeMapperType = typeof(DefaultEntityActionHasResponseRequestHandler<,>)
                .MakeGenericType(RequestType!, ResponseType!);

        if (HasKey && !HasResponseData)
            routeMapperType = typeof(DefaultEntityActionRequestHandler<,>)
                .MakeGenericType(RequestType!, KeyProperty!.PropertyType);

        if (!HasKey && !HasResponseData)
            routeMapperType = typeof(DefaultEntityActionRequestHandler<>)
                .MakeGenericType(RequestType!);

        if (routeMapperType is null)
            throw new InvalidOperationException("Invalid route mapper type");

        services.AddKeyedSingleton(this, (s, k) => (IRouteMapper)ActivatorUtilities.CreateInstance(s, routeMapperType, k));
    }
}
