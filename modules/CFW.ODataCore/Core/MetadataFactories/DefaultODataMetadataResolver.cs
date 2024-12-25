using CFW.ODataCore.Core.Attributes;
using System.Reflection;

namespace CFW.ODataCore.Core.MetadataResolvers;

public class DefaultODataMetadataResolver : BaseODataMetadataResolver
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttributes<EndpointAttribute>().Any())
        .ToList();

    public DefaultODataMetadataResolver(string defaultPrefix) : base(defaultPrefix)
    {
    }

    protected override IEnumerable<Type> CachedType => _cachedType;

}
