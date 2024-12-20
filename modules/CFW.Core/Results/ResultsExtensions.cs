﻿namespace CFW.Core.Results;

public static class ResultsExtensions
{
    public static Result Success(this object _)
        => new Result { IsSuccess = true };

    public static Result<T> Success<T>(this T? data)
        => new Result<T> { IsSuccess = true, Data = data, HttpStatusCode = System.Net.HttpStatusCode.OK };

    public static Result<T> Created<T>(this T data)
        => new Result<T> { IsSuccess = true, Data = data, HttpStatusCode = System.Net.HttpStatusCode.Created };

    public static Result Failed(this object _, string message)
        => new Result { IsSuccess = false, Message = message, HttpStatusCode = System.Net.HttpStatusCode.BadRequest };

    public static Result<T> Notfound<T>(this T? _, string? message = null)
        => new Result<T> { IsSuccess = false, Message = message, HttpStatusCode = System.Net.HttpStatusCode.NotFound };
}
