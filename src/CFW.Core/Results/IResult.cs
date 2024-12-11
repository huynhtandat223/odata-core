using Microsoft.AspNetCore.Mvc;

namespace CFW.Core.Results;

public class Result
{
    public bool IsSuccess { get; set; }

    public string? Message { get; set; }

    public Exception? Exception { get; set; }

    public ActionResult ToActionResult()
    {
        return IsSuccess
            ? new OkResult()
            : new BadRequestObjectResult(Message);
    }
}

public class Result<T> : Result
{
    public T? Data { get; set; }
}
