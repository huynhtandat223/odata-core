using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;

namespace CFW.ODataCore;

public class EntityMimimalApiOptions
{
    internal Type DefaultDbContext { get; set; } = default!;

    internal ServiceLifetime DbServiceLifetime { get; set; } = ServiceLifetime.Scoped;

    internal Action<ODataOptions> ODataOptions { get; set; } = (options) => options.EnableQueryFeatures();

    internal MetadataContainerFactory MetadataContainerFactory { get; set; } = new MetadataContainerFactory();

    internal Action<ODataConventionModelBuilder>? ConfigureModelBuilder { get; set; }

    internal Action<RouteGroupBuilder>? ConfigureContainerRouteGroup { get; set; }

    /// <summary>
    /// Use default DbContext when use EntityAttribute without specify entity configuration
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <param name="serviceLifetime"></param>
    /// <returns></returns>
    public EntityMimimalApiOptions UseDefaultDbContext<TDbContext>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        DefaultDbContext = typeof(TDbContext);
        DbServiceLifetime = serviceLifetime;

        return this;
    }

    /// <summary>
    /// Configue OData options
    /// </summary>
    /// <param name="odataOptions"></param>
    /// <returns></returns>
    public EntityMimimalApiOptions UseODataOptions(Action<ODataOptions> odataOptions)
    {
        ODataOptions = odataOptions;
        return this;
    }

    /// <summary>
    /// Assemply container hold necessary cached types for api generation, you can manually configue types to this container
    /// </summary>
    /// <param name="metadataContainerFactory"></param>
    /// <returns></returns>
    public EntityMimimalApiOptions UseMetadataContainerFactory(MetadataContainerFactory metadataContainerFactory)
    {
        MetadataContainerFactory = metadataContainerFactory;
        return this;
    }

    /// <summary>
    /// Configure OData model builder after all entities, operations configured
    /// </summary>
    /// <param name="configureModelBuilder"></param>
    /// <returns></returns>
    public EntityMimimalApiOptions ConfigureODataModelBuilder(Action<ODataConventionModelBuilder> configureModelBuilder)
    {
        ConfigureModelBuilder = configureModelBuilder;
        return this;
    }

    public EntityMimimalApiOptions ConfigureMinimalApiContainerRouteGroup(Action<RouteGroupBuilder> configureContainerRouteGroup)
    {
        ConfigureContainerRouteGroup = configureContainerRouteGroup;
        return this;
    }
}
