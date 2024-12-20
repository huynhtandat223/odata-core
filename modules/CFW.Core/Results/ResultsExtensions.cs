namespace CFW.Core.Results;

public static class ResultsExtensions
{
    public static Result Success(this object _)
        => new Result { IsSuccess = true };

    public static Result<T> Success<T>(this T data)
        => new Result<T> { IsSuccess = true, Data = data };

    public static Result<T> Created<T>(this T data)
        => new Result<T> { IsSuccess = true, Data = data, IsCreated = true };

    public static Result Failed(this object _, string message)
        => new Result { IsSuccess = false, Message = message };
}
