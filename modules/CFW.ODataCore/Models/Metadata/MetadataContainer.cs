namespace CFW.ODataCore.Models.Metadata;

public class MetadataContainer
{
    public string RoutePrefix { get; init; }

    public IList<MetadataEntity> MetadataEntities { get; } = new List<MetadataEntity>();

    public EntityMimimalApiOptions Options { get; init; }

    public IServiceProvider? ODataInternalServiceProvider { get; set; }

    public IList<MetadataUnboundAction> UnboundOperations { get; set; }
        = new List<MetadataUnboundAction>();

    public MetadataContainer(string routePrefix, EntityMimimalApiOptions options)
    {
        RoutePrefix = routePrefix;
        Options = options;
    }
}
