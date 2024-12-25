using CFW.ODataCore.Features.EFCore;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Edm;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore.Extensions;

public abstract class ODataRequestHandler
{
    private ODataQuerySettings _querySettings = new ODataQuerySettings();

    public ODataQueryOptions AddODataFeature(HttpRequest request, Type clrType, IEdmModel model)
    {
        var odataOptions = request.HttpContext.RequestServices.GetRequiredService<IOptions<ODataOptions>>().Value;

        var edmEntitySet = model.EntityContainer.FindEntitySet("categories");
        var entitySetSegment = new EntitySetSegment(edmEntitySet);
        var routeComponent = odataOptions.RouteComponents.FirstOrDefault();
        var feature = new ODataFeature
        {
            Path = new ODataPath(entitySetSegment),
            Model = model,
            RoutePrefix = routeComponent.Key,
        };
        request.HttpContext.Features.Set<IODataFeature>(feature);


        ODataPath path = request.ODataFeature().Path;
        _querySettings.TimeZone = request.GetTimeZoneInfo();

        IEdmType edmType = path.GetEdmType();

        // When $count is at the end, the return type is always int. Trying to instead fetch the return type of the actual type being counted on.
        if (request.IsCountRequest())
        {
            ODataPathSegment[] pathSegments = path.ToArray();
            edmType = pathSegments[pathSegments.Length - 2].EdmType;
        }

        IEdmType elementType = edmType.AsElementType();
        IEdmModel edmModel = request.GetModel();

        var queryContext = new ODataQueryContext(edmModel, clrType, path);
        return new ODataQueryOptions(queryContext, request);
    }

    public async Task WriteFormattedResponseAsync(HttpContext context, object responseObject)
    {
        var formatters = ODataOutputFormatterFactory.Create();
        var formatterContext = new OutputFormatterWriteContext(
            context,
            (stream, encoding) => new StreamWriter(stream, encoding),
            responseObject?.GetType() ?? typeof(object),
            responseObject
        )
        {
            ContentType = context.Request.ContentType
        };

        // Select an appropriate formatter based on the Accept header
        var selectedFormatter = formatters
            .OfType<OutputFormatter>()
            .FirstOrDefault(f => f.CanWriteResult(formatterContext));

        if (selectedFormatter != null)
            await selectedFormatter.WriteAsync(formatterContext);
        else
        {
            // Handle case where no formatter is found
            context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
            await context.Response.WriteAsync("No suitable formatter found.");
        }
    }

    public abstract Task Execute(HttpRequest httpRequest, string routePrefix, string routeName);
}

public class ODataRequestHandler<TViewModel> : ODataRequestHandler
    where TViewModel : class
{
    private readonly IODataDbContextProvider _contextProvider;
    private readonly ODataMetadataContainer _container;

    public ODataRequestHandler(IODataDbContextProvider contextProvider, ODataMetadataContainer container)
    {
        _contextProvider = contextProvider;
        _container = container;
    }

    public override async Task Execute(HttpRequest request, string routePrefix, string routeName)
    {
        var queryOptions = AddODataFeature(request, typeof(TViewModel), _container.EdmModel);

        var db = _contextProvider.GetContext();
        var query = db.Set<TViewModel>().AsQueryable();
        var applied = queryOptions.ApplyTo(query);

        await WriteFormattedResponseAsync(request.HttpContext, applied);
    }
}
