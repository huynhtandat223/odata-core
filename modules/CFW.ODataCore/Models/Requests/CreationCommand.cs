using CFW.ODataCore.Models.Metadata;
using CFW.ODataCore.RouteMappers;

namespace CFW.ODataCore.Models.Requests;

public class CreationCommand<TEntity>
{
    public EntityDelta<TEntity> Delta { get; init; }

    public MetadataEntity Metadata { get; init; }

    public CreationCommand(EntityDelta<TEntity> delta, MetadataEntity metadata)
    {
        Delta = delta;
        Metadata = metadata;
    }
}
