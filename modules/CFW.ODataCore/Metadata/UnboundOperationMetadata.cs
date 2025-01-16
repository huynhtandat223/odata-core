using CFW.ODataCore.Attributes;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CFW.ODataCore.ODataMetadata;

[Obsolete]
public record class UnboundOperationMetadata
{
    public required OperationType OperationType { get; set; }

    public required string OperationName { get; set; }

    public required ServiceDescriptor ServiceDescriptor { get; set; }

    public required Type RequestType { get; set; }

    public required Type ResponseType { get; set; }

    private static Type[] _unboundOperationInterfaceDefinitions
        = [typeof(IUnboundOperationHandler<>), typeof(IUnboundOperationHandler<,>)];

    public bool IsKeyedOperation(out PropertyInfo? propertyInfo)
    {
        var keyProp = RequestType
            .GetProperties()
            .FirstOrDefault(x => x.GetCustomAttributes<KeyAttribute>().SingleOrDefault() is not null
                || x.GetCustomAttributes<FromRouteAttribute>().Any());

        propertyInfo = keyProp;

        if (keyProp is null)
            return false;

        return true;
    }

    internal static UnboundOperationMetadata Create(Type targetType, UnboundOperationAttribute attribute)
    {
        var implemnationInterface = targetType.GetInterfaces().Where(x => x.IsGenericType)
            .Where(x => _unboundOperationInterfaceDefinitions.Contains(x.GetGenericTypeDefinition()))
            .SingleOrDefault();
        if (implemnationInterface == null)
            throw new InvalidOperationException($"Operation handler {targetType.FullName} not implement any unbound operation interface");


        var operationName = attribute.OperationName.Trim();
        var operationType = attribute.OperationType;

        var interfaceGenericArgs = implemnationInterface.GetGenericArguments();
        var isNonResponse = interfaceGenericArgs.Count() == 1;
        var requestType = interfaceGenericArgs[0];
        var responseType = isNonResponse ? typeof(Result) : interfaceGenericArgs[1];

        var serviceDescriptor = new ServiceDescriptor(implemnationInterface, targetType, ServiceLifetime.Transient);
        return new UnboundOperationMetadata
        {
            OperationType = operationType,
            OperationName = operationName,
            ServiceDescriptor = serviceDescriptor,
            RequestType = requestType,
            ResponseType = responseType,
        };
    }
}


