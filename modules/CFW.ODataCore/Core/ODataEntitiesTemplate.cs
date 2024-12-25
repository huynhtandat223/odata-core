using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore.Core;

[Obsolete("Use minimal api")]
internal class ODataEntitiesTemplate : ODataSegmentTemplate
{
    private readonly EntitySetSegment _entitySetSegment;
    private readonly bool _ignoreKeyTemplates;

    public ODataEntitiesTemplate(IEdmEntitySet edmEntitySet, bool ignoreKeyTemplates)
    {
        _entitySetSegment = new EntitySetSegment(edmEntitySet);
        _ignoreKeyTemplates = ignoreKeyTemplates;
    }

    public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
    {
        if (_ignoreKeyTemplates)
            yield return $"/{_entitySetSegment.EntitySet.Name}";
        else
        {
            yield return $"/{_entitySetSegment.EntitySet.Name}/{{key}}";
            yield return $"/{_entitySetSegment.EntitySet.Name}({{key}})";
        }
    }

    public override bool TryTranslate(ODataTemplateTranslateContext context)
    {
        context.Segments.Add(_entitySetSegment);
        if (_ignoreKeyTemplates)
            return true;

        if (!context.RouteValues.TryGetValue("key", out var key))
            throw new InvalidOperationException("Key not found in route values.");


        //NEt 9.0
        // var keyName = _entitySetSegment.EntitySet.EntityType.DeclaredKey.Single();
        var entityType = _entitySetSegment.EntitySet.EntityType();
        var keyName = entityType.DeclaredKey.Single();

        var keySegment = new KeySegment(new Dictionary<string, object> { { keyName.Name, key! } }, entityType
            , _entitySetSegment.EntitySet);
        context.Segments.Add(keySegment);

        return true;
    }
}
