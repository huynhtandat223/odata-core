using CFW.Core.Testings.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace CFW.ODataCore.Testings.TestCases;

public abstract class BaseTests
{
    protected readonly ITestOutputHelper _testOutputHelper;
    protected readonly WebApplicationFactory<Program> _factory;

    public BaseTests(ITestOutputHelper testOutputHelper, WebApplicationFactory<Program> factory)
    {
        _testOutputHelper = testOutputHelper;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.Services.AddSingleton<ILoggerProvider>(r
                        => new XunitLoggerProvider(_testOutputHelper, "Testing"));
                });
        });
    }
}
