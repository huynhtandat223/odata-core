using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;

namespace CFW.ODataCore.Models;

public class ODataResults<T> : IResult
{
    public T? Data { get; set; }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        await WriteFormattedResponseAsync(httpContext, Data!);
    }

    private static async Task WriteFormattedResponseAsync(HttpContext context, object responseObject)
    {
        var outputFormatters = context.RequestServices.GetRequiredService<IEnumerable<ODataOutputFormatter>>();

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
        var selectedFormatter = outputFormatters
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
}
