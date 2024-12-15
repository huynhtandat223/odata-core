using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result)
    {
        return result.IsSuccess
            ? new OkResult()
            : new BadRequestObjectResult(result.Message);
    }
}
