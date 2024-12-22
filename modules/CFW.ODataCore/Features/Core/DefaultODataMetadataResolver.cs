using CFW.ODataCore.Features.BoundOperations;
using CFW.ODataCore.Features.EntitySets;
using CFW.ODataCore.Features.UnBoundActions;
using CFW.ODataCore.Features.UnboundFunctions;
using System.Reflection;

namespace CFW.ODataCore.Features.Core;

public class DefaultODataMetadataResolver : BaseODataMetadataResolver
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttribute<ODataEntitySetAttribute>() is not null
            || x.GetCustomAttribute<BoundOperationAttribute>() is not null
            || x.GetCustomAttribute<UnboundActionAttribute>() is not null
            || x.GetCustomAttribute<UnboundFunctionAttribute>() is not null)
        .ToList();

    public DefaultODataMetadataResolver(string defaultPrefix) : base(defaultPrefix)
    {
    }

    protected override IEnumerable<Type> CachedType => _cachedType;

}
