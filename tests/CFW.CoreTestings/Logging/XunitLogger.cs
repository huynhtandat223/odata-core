using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace CFW.CoreTestings.Logging;

[DebuggerStepThrough]
public sealed class XunitLogger : ILogger
{
    private const string ScopeDelimiter = "=> ";
    private const string Spacer = "      ";

    private const string Trace = "trce";
    private const string Debug = "dbug";
    private const string Info = "info";
    private const string Warn = "warn";
    private const string Error = "fail";
    private const string Critical = "crit";

    private readonly string _categoryName;
    private readonly bool _useScopes;
    private readonly string _service;
    private readonly ITestOutputHelper _output;
    private readonly IExternalScopeProvider _scopes;

    private static string[] _disableCategories = new[] {
        "Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware",
        "Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker",
        "Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor",
        "Microsoft.AspNetCore.Routing.EndpointMiddleware" };

    public XunitLogger(ITestOutputHelper output
        , IExternalScopeProvider scopes, string categoryName, bool useScopes, string service)
    {
        _output = output;
        _scopes = scopes;
        _categoryName = categoryName;
        _useScopes = useScopes;
        _service = service;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return !_disableCategories.Contains(_categoryName);
    }

#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
    public IDisposable BeginScope<TState>(TState state)
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
    {
        return _scopes.Push(state);
    }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        Func<TState, Exception, string> formatter)
    {
        //if (logLevel == LogLevel.Debug || logLevel == LogLevel.Trace || logLevel == LogLevel.Information)
        //    return;

        var sb = new StringBuilder();

        sb.Append($"[{_service}] ");
        switch (logLevel)
        {
            case LogLevel.Trace:
                sb.Append(Trace);
                break;
            case LogLevel.Debug:
                sb.Append(Debug);
                break;
            case LogLevel.Information:
                sb.Append(Info);
                break;
            case LogLevel.Warning:
                sb.Append(Warn);
                break;
            case LogLevel.Error:
                sb.Append(Error);
                break;
            case LogLevel.Critical:
                sb.Append(Critical);
                break;
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }

        sb.Append(": ").Append(_categoryName).Append('[').Append(eventId).Append(']').AppendLine();

        if (_useScopes && TryAppendScopes(sb))
            sb.AppendLine();

        sb.Append(Spacer);
        sb.Append(formatter(state, exception));

        if (exception != null)
        {
            sb.AppendLine();
            sb.Append(Spacer);
            sb.Append(exception);
        }

        try
        {
            var message = sb.ToString();

            if (message.Contains(""))

                _output.WriteLine(message);
        }
        catch
        {

        }

    }

    private bool TryAppendScopes(StringBuilder sb)
    {
        var scopes = false;
        _scopes.ForEachScope((callback, state) =>
        {
            if (!scopes)
            {
                state.Append(Spacer);
                scopes = true;
            }
            state.Append(ScopeDelimiter);
            state.Append(callback);
        }, sb);
        return scopes;
    }
}
