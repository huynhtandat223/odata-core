using CFW.CoreTestings.Logging;
using Microsoft.AspNetCore.Identity;

namespace CFW.ODataCore.Testings.TestCases;

public class SeedUserInfo
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string[]? Roles { get; set; }
}

public abstract class BaseTests
{
    protected readonly ITestOutputHelper _testOutputHelper;
    protected WebApplicationFactory<Program> _factory;

    public const string DefaultPassword = "123!@#abcABC";

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

    protected async Task SeedUsers(IEnumerable<SeedUserInfo> seedUserInfos)
    {
        var userManager = _factory.Services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = _factory.Services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var seedUserInfo in seedUserInfos)
        {
            var creatingUser = new IdentityUser { UserName = seedUserInfo.UserName };
            var result = await userManager.CreateAsync(creatingUser, seedUserInfo.Password);
            result.Succeeded.Should().BeTrue();

            if (seedUserInfo.Roles != null)
            {
                var user = await userManager.FindByNameAsync(seedUserInfo.UserName);
                user.Should().NotBeNull();

                foreach (var role in seedUserInfo.Roles)
                {
                    var roleExists = await roleManager.RoleExistsAsync(role);
                    if (!roleExists)
                    {
                        var creatingRole = new IdentityRole { Name = role };
                        var roleResult = await roleManager.CreateAsync(creatingRole);
                        roleResult.Succeeded.Should().BeTrue();
                    }
                }

                await userManager.AddToRolesAsync(user!, seedUserInfo.Roles);
            }
        }
    }

    protected async Task SeedUser(string userName, string password)
    {
        var userManager = _factory.Services.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = userName };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Test data invalid. User creation failed.");
    }
}
