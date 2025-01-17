using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Web;

namespace CFW.ODataCore.Models;

public class QueryRequest<TRequest>
{
    public TRequest? QueryModel { get; set; }

    public static ValueTask<QueryRequest<TRequest>> BindAsync(HttpContext context)
    {
        var request = context.Request;
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;
        var result = new QueryRequest<TRequest>();

        if (!request.QueryString.HasValue)
            return new ValueTask<QueryRequest<TRequest>>(result);

        var dict = HttpUtility.ParseQueryString(request.QueryString.Value);
        string json = JsonSerializer.Serialize(dict.Cast<string>().ToDictionary(k => k, v => dict[v]));
        var model = JsonSerializer.Deserialize<TRequest>(json, jsonOptions.JsonSerializerOptions);

        result.QueryModel = model;
        return new ValueTask<QueryRequest<TRequest>>(result);
    }

}


