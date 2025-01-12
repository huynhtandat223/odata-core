using CFW.Core.Entities;
using CFW.CoreTestings.Logging;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OData.ModelBuilder;

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
    protected List<object> requestObjects = new List<object>();

    public const string DefaultPassword = "123!@#abcABC";

    public const string DefaultIdProp = nameof(IEntity<Guid>.Id);

    public BaseTests(ITestOutputHelper testOutputHelper, AppFactory factory
        , string? odataPrefix = null, Type[]? types = null)
    {
        _testOutputHelper = testOutputHelper;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var dbDir = Path.Combine(currentDirectory, "testDbs");
                if (!Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }
                var dbPath = Path.Combine(dbDir, $"appdbcontext_{Guid.NewGuid()}.db");
                services.AddDbContext<TestingDbContext>(
                           options => options
                           .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<TestingDbContext>>()
                           .EnableSensitiveDataLogging()
                           .UseSqlite($"Data Source={dbPath}"));

                services
                    .AddEntityMinimalApi(o =>
                    {
                        o.UseDefaultDbContext<TestingDbContext>()
                        .ConfigureODataModelBuilder(b => b.EnableLowerCamelCase());

                        if (types != null)
                        {
                            o.UseMetadataContainerFactory(new TestMetadataContainerFactory(types));
                        }
                    }, defaultRoutePrefix: odataPrefix ?? Constants.DefaultODataRoutePrefix);
                services.AddSingleton(requestObjects);
            }).ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.Services.AddSingleton<ILoggerProvider>(r
                    => new XunitLoggerProvider(_testOutputHelper, "Testing"));
            }); ;
        });
    }

    protected async Task SeedUsers(IEnumerable<SeedUserInfo> seedUserInfos)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
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
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = userName };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Test data invalid. User creation failed.");
    }

    protected TestingDbContext GetDbContext()
    {
        return _factory.Services.CreateScope().ServiceProvider.GetRequiredService<TestingDbContext>();
    }

    public async Task<List<object>> SeedData(Type dbType, int count, TestingDbContext? db = null)
    {
        db ??= GetDbContext();
        var data = DataGenerator.CreateList(dbType, count);
        foreach (var item in data)
        {
            db.Add(item);
        }
        await db.SaveChangesAsync();
        return data.OfType<object>().ToList();
    }

    public List<object> SeedData(Type dbModelType, int dataCount, IServiceCollection services)
    {
        var db = services.BuildServiceProvider().GetService<TestingDbContext>();
        if (!db!.Database.CanConnect())
            db.Database.EnsureCreated();
        var task = SeedData(dbModelType, dataCount, db);
        task.Wait();

        return task.Result;
    }

}
