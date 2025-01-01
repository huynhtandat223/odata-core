using CFW.ODataCore.Attributes;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CFW.ODataCore.ODataMetadata;

public class EntityOperationMetadata
{
    public required OperationType OperationType { get; set; }

    public required string EntityRoutingName { get; set; }

    public required string OperationName { get; set; }

    public required ServiceDescriptor ServiceDescriptor { get; set; }

    public required Type RequestType { get; set; }

    public required Type ResponseType { get; set; }

    public bool IsKeyedOperation<TKey>(out PropertyInfo? propertyInfo)
    {
        var keyProp = RequestType
            .GetProperties()
            .Where(x => x.CanWrite && x.PropertyType == typeof(TKey))
            .FirstOrDefault(x => x.GetCustomAttributes<KeyAttribute>().SingleOrDefault() is not null
                || x.GetCustomAttributes<FromRouteAttribute>().Any());

        propertyInfo = keyProp;

        return keyProp is not null;
    }

    private static Type[] _entityOperationInterfaceDefinitions = [typeof(IEntityOperationHandler<,>), typeof(IEntityOperationHandler<,,>)];


    internal static EntityOperationMetadata Create(Type entityType, string entytRoutingName
        , Type targetType, BoundOperationAttribute attribute)
    {
        var implemnationInterface = targetType.GetInterfaces().Where(x => x.IsGenericType)
            .Where(x => _entityOperationInterfaceDefinitions.Contains(x.GetGenericTypeDefinition()))
            .Where(x => x.GetGenericArguments().First() == entityType)
            .SingleOrDefault();

        if (implemnationInterface is null)
            throw new InvalidOperationException($"Operation handler {targetType.FullName} " +
                $"not implement any operation interface");

        var operationType = attribute.OperationType;
        var interfaceGenericArgs = implemnationInterface.GetGenericArguments();
        var isNonResponse = interfaceGenericArgs.Count() == 2;
        var requestType = interfaceGenericArgs[1];
        var responseType = isNonResponse ? typeof(Result) : interfaceGenericArgs[2];
        var serviceType = isNonResponse
            ? typeof(IEntityOperationHandler<,>).MakeGenericType(entityType, requestType)
            : typeof(IEntityOperationHandler<,,>).MakeGenericType(entityType, requestType, responseType);

        var serviceDescriptor = new ServiceDescriptor(serviceType, targetType, ServiceLifetime.Transient);

        return new EntityOperationMetadata
        {
            ResponseType = responseType,
            RequestType = requestType,
            OperationType = operationType,
            EntityRoutingName = entytRoutingName,
            OperationName = attribute.OperationName,
            ServiceDescriptor = serviceDescriptor,
        };
    }
}


