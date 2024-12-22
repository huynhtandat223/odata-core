using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore.Features.UnBoundActions;

public class UnboundOperationTemplate : ODataSegmentTemplate
{
    private readonly IEdmOperation _opration;

    public UnboundOperationTemplate(IEdmOperation opration)
    {
        _opration = opration;
    }

    public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
    {
        yield return $"/{_opration.Name}";
    }

    public override bool TryTranslate(ODataTemplateTranslateContext context)
    {
        context.Segments.Add(new OperationSegment(_opration, null));
        return true;
    }
}
