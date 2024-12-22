using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore.Features.BoundActions;

internal class BoundActionsTemplate : ODataSegmentTemplate
{
    private readonly EntitySetSegment _entitySetSegment;
    private readonly bool _ignoreKeyTemplates;
    private readonly IEdmAction _edmAction;

    public BoundActionsTemplate(IEdmEntitySet edmEntitySet
        , bool ignoreKeyTemplates
        , IEdmAction edmAction)
    {
        _entitySetSegment = new EntitySetSegment(edmEntitySet);
        _ignoreKeyTemplates = ignoreKeyTemplates;
        _edmAction = edmAction;
    }

    public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
    {
        if (_ignoreKeyTemplates)
            yield return $"/{_entitySetSegment.EntitySet.Name}/{_edmAction.Name}";
        else
        {
            yield return $"/{_entitySetSegment.EntitySet.Name}/{{key}}/{_edmAction.Name}";
            yield return $"/{_entitySetSegment.EntitySet.Name}({{key}})/{_edmAction.Name}";
        }
    }

    public override bool TryTranslate(ODataTemplateTranslateContext context)
    {
        context.Segments.Add(_entitySetSegment);

        if (context.RouteValues.TryGetValue("key", out var key))
        {
            var entityType = _entitySetSegment.EntitySet.EntityType();
            var keyName = entityType.DeclaredKey.Single();

            var keySegment = new KeySegment(new Dictionary<string, object> { { keyName.Name, key! } }, entityType
                , _entitySetSegment.EntitySet);
            context.Segments.Add(keySegment);
        }

        context.Segments.Add(new OperationSegment(_edmAction, null));
        return true;
    }
}