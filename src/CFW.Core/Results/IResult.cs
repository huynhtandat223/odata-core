namespace CFW.Core.Results;

public record Result
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public Exception? Exception { get; init; }
}
