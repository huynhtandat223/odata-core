
namespace CFW.ODataCore.Testings;

public class TestODataTypeResolver : BaseODataTypeResolver
{
    public TestODataTypeResolver(string defaultRoutePrefix, Type[] cacheTypes) : base(defaultRoutePrefix)
    {
        CachedType = cacheTypes;
    }

    protected override IEnumerable<Type> CachedType { get; }
}
