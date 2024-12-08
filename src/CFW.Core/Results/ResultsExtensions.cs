namespace CFW.Core.Results;

public static class ResultsExtensions
{
    public static Result Ok(this object _)
        => new Result { Success = true };
}
