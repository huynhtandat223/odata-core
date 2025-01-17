using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CFW.ODataCore.Models.Metadata;

public abstract class MetadataAction
{
    public required Type TargetType { get; init; }

    public required string ActionName { get; init; }

    public ApiMethod HttpMethod { get; set; } = ApiMethod.Post;

    public required string RoutePrefix { get; init; }

    public required Type ImplementedInterface { get; init; }

    public Type? RequestType { get; private set; }

    public Type? ResponseType { get; private set; }

    public PropertyInfo? KeyProperty { get; private set; }

    protected bool HasKey => KeyProperty is not null;

    protected bool HasResponseData => ResponseType != typeof(Result);

    protected void ResolveRequestResponseTypes()
    {
        var args = ImplementedInterface.GetGenericArguments();
        if (args.Length > 2)
            throw new InvalidOperationException($"Invalid generic arguments count {args.Length} for {ImplementedInterface.FullName}");

        RequestType = args[0];
        ResponseType = args.Length == 1 ? typeof(Result) : args[1];

        KeyProperty = RequestType.GetProperties()
            .SingleOrDefault(x => x.GetCustomAttribute<KeyAttribute>() is not null);
    }
}
