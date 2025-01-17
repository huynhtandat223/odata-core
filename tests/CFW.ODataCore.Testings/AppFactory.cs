[assembly: TestFramework(AssemblyFixtureFramework.TypeName, AssemblyFixtureFramework.AssemblyName)]

namespace CFW.ODataCore.Testings;

public class AppFactory : WebApplicationFactory<Program>, IDisposable
{
    public AppFactory()
    {
        var dbDirectory = Path.Combine(Directory.GetCurrentDirectory(), "testDbs");
        if (Directory.Exists(dbDirectory))
        {
            Directory.Delete(dbDirectory, true);
        }
    }

    public const string TestingEnvironment = "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment(TestingEnvironment);
        builder.UseContentRoot(Directory.GetCurrentDirectory());

    }
}
