namespace CFW.ODataCore.Testings;

public class TestMetadataContainerFactory : MetadataContainerFactory
{
    public TestMetadataContainerFactory(params Type[] types)
    {
        CacheType = types;
    }
}
