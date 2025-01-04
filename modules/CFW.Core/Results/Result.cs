using System.Net;

namespace CFW.Core.Results;

public class Result
{
    public bool IsSuccess { get; set; }

    public string? Message { get; set; }

    public Exception? Exception { get; set; }

    public HttpStatusCode HttpStatusCode { get; set; }

    public object? Data { get; set; }

    /// <summary>
    /// Return directly the custom result if it is not null.
    /// </summary>
    public object? CustomResult { get; set; }
}

public class Result<T> : Result
{
    public new T? Data
    {
        get => (T?)base.Data;
        set => base.Data = value;
    }
}
