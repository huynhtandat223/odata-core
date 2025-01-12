using CFW.ODataCore.Models;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore.RequestHandlers;

public abstract class EntityMetadata
{
    public IODataFeature Feature => _odataFeature.Value;

    public ODataMetadataContainer Container { get; }

    public required string EndpointName { get; init; }

    protected readonly Lazy<IODataFeature> _odataFeature;

    internal EntityMetadata(ODataMetadataContainer container)
    {
        Container = container;

        _odataFeature = new(() =>
        {
            var model = Container.EdmModel;
            var edmEntitySet = model.EntityContainer.FindEntitySet(EndpointName);
            var entitySetSegment = new EntitySetSegment(edmEntitySet);
            var segments = new List<ODataPathSegment> { entitySetSegment };

            var path = new ODataPath(segments);
            var feature = new ODataFeature
            {
                Path = path,
                Model = model,
                RoutePrefix = Container.RoutePrefix,
            };

            return feature;
        });
    }

    internal AllowedQueryOptions IgnoreQueryOptions { get; init; }
}
