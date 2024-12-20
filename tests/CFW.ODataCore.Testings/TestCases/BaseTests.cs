using CFW.Core.Testings.Logging;
using Microsoft.AspNetCore.Identity;

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

    protected async Task SeedUser(string userName, string password)
    {
        var userManager = _factory.Services.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = "admin" };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Test data invalid. User creation failed.");
    }
}
