using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore.Features.UnBoundActions;

public class UnboundActionsTemplate : ODataSegmentTemplate
{
    private readonly IEdmAction _action;

    public UnboundActionsTemplate(IEdmAction action)
    {
        _action = action;
    }

    public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
    {
        yield return $"/{_action.Name}";
    }

    public override bool TryTranslate(ODataTemplateTranslateContext context)
    {
        context.Segments.Add(new OperationSegment(_action, null));
        return true;
    }
}
