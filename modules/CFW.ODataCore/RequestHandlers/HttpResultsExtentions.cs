
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RequestHandlers;

public static class HttpResultsExtentions
{
    public static ObjectResult ToResults(this Result result)
    {
        var objectResult = new ObjectResult(result);
        objectResult.StatusCode = (int)result.HttpStatusCode;
        return objectResult;
    }
}


