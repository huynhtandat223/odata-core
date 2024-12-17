using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace CFW.Core.Testings.Logging;
public sealed class XunitLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ITestOutputHelper _output;
    private readonly bool _useScopes;
    private readonly string _service;
    private IExternalScopeProvider _scopes;

    public XunitLoggerProvider(ITestOutputHelper output, string service)
    {
        _output = output;
        _useScopes = false;
        _service = service;
        _scopes = default!;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(_output, _scopes, categoryName, _useScopes, _service);
    }

    public void Dispose()
    {
    }

    public void SetScopeProvider(IExternalScopeProvider scopes)
    {
        _scopes = scopes;
    }
}
