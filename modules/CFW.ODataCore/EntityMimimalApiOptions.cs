using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore;

public class EntityMimimalApiOptions
{
    public bool IsLazyBuildMetadata { get; set; }

    internal Type DefaultDbContext { get; set; } = default!;

    internal ServiceLifetime DbServiceLifetime { get; set; } = ServiceLifetime.Scoped;

    public Action<ODataOptions> ODataOptions { get; set; } = (options) => options.EnableQueryFeatures();

    public MetadataContainerFactory MetadataContainerFactory { get; set; } = new MetadataContainerFactory();

    public EntityMimimalApiOptions UseDefaultDbContext<TDbContext>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        DefaultDbContext = typeof(TDbContext);
        DbServiceLifetime = serviceLifetime;

        return this;
    }

    public EntityMimimalApiOptions UseODataOptions(Action<ODataOptions> odataOptions)
    {
        ODataOptions = odataOptions;
        return this;
    }

    public EntityMimimalApiOptions UseMetadataContainerFactory(MetadataContainerFactory metadataContainerFactory)
    {
        MetadataContainerFactory = metadataContainerFactory;
        return this;
    }

    public EntityMimimalApiOptions UseLazyBuildMetadata(bool useLazyStartup = true)
    {
        IsLazyBuildMetadata = useLazyStartup;
        return this;
    }
}
