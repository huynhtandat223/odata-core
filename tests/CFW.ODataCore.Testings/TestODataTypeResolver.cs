
using CFW.ODataCore.Features.Core;

namespace CFW.ODataCore.Testings;

public class TestODataTypeResolver : BaseODataMetadataResolver
{
    public TestODataTypeResolver(string defaultRoutePrefix, Type[] cacheTypes) : base(defaultRoutePrefix)
    {
        CachedType = cacheTypes;
    }

    protected override IEnumerable<Type> CachedType { get; }
}
