using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Results;

namespace CFW.ODataCore.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result)
    {
        return result.IsSuccess
            ? new OkResult()
            : new BadRequestObjectResult(result.Message);
    }

    public static ActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsCreated == true)
        {
            return new CreatedODataResult<T>(result.Data!);
        }

        return ToActionResult(result);
    }
}
