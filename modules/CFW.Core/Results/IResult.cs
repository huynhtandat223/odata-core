using System.Net;

namespace CFW.Core.Results;

public class Result
{
    public bool IsSuccess { get; set; }

    public string? Message { get; set; }

    public Exception? Exception { get; set; }

    public HttpStatusCode HttpStatusCode { get; set; }
}

public class Result<T> : Result
{
    public T? Data { get; set; }
}
