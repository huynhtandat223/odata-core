using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore.Features.BoundOperations;

internal class BoundOperationTemplate : ODataSegmentTemplate
{
    private readonly EntitySetSegment _entitySetSegment;
    private readonly bool _ignoreKeyTemplates;
    private readonly IEdmOperation _operation;

    public BoundOperationTemplate(IEdmEntitySet edmEntitySet
        , bool ignoreKeyTemplates
        , IEdmOperation operation)
    {
        _entitySetSegment = new EntitySetSegment(edmEntitySet);
        _ignoreKeyTemplates = ignoreKeyTemplates;
        _operation = operation;
    }

    public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
    {
        if (_ignoreKeyTemplates)
            yield return $"/{_entitySetSegment.EntitySet.Name}/{_operation.Name}";
        else
        {
            yield return $"/{_entitySetSegment.EntitySet.Name}/{{key}}/{_operation.Name}";
            yield return $"/{_entitySetSegment.EntitySet.Name}({{key}})/{_operation.Name}";
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

        context.Segments.Add(new OperationSegment(_operation, null));
        return true;
    }
}