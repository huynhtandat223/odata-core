using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result)
    {
        var statusCodeResult = new StatusCodeResult((int)result.HttpStatusCode);
        return statusCodeResult;
    }

    public static ActionResult ToActionResult<T>(this Result<T> result)
    {
        var objectResult = new ObjectResult(result.Data);
        objectResult.StatusCode = (int)result.HttpStatusCode;

        return objectResult;
    }
}
