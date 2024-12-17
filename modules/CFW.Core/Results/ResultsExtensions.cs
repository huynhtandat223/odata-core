namespace CFW.Core.Results;

public static class ResultsExtensions
{
    public static Result Ok(this object _)
        => new Result { IsSuccess = true };

    public static Result<T> Ok<T>(this T data)
        => new Result<T> { IsSuccess = true, Data = data };

    public static Result Failed(this object _, string message)
        => new Result { IsSuccess = false, Message = message };
}
