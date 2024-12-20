


using CFW.ODataCore.Extensions;
using Microsoft.AspNetCore.TestHost;

namespace CFW.ODataCore.Testings;

public class NonInitAppFactory : WebApplicationFactory<Program>, IDisposable
{
    public const string TestingEnvironment = "Testing";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment(TestingEnvironment);
        builder.UseContentRoot(Directory.GetCurrentDirectory());
    }
    protected override TestServer CreateServer(IWebHostBuilder builder)
    {
        var server = base.CreateServer(builder);
        return server;
    }
}

public class AppFactory : WebApplicationFactory<Program>, IDisposable
{
    public const string TestingEnvironment = "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseEnvironment(TestingEnvironment);
        builder.UseContentRoot(Directory.GetCurrentDirectory());

        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddGenericODataEndpoints();
        });
    }

    protected override TestServer CreateServer(IWebHostBuilder builder)
    {
        var server = base.CreateServer(builder);

        return server;
    }
}
