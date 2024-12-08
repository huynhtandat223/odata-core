using CFW.Core.Entities;
using CFW.ODataCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CFW.ODataCore.Tests;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private static string _assemplyName = typeof(DesignTimeDbContextFactory).Assembly.GetName().Name ?? string.Empty;

    public AppDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json")
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString, s => s.MigrationsAssembly(_assemplyName));

        var assemblies = new[] { typeof(DesignTimeDbContextFactory).Assembly };
        var types = assemblies.SelectMany(a => a.GetTypes())
                   .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>)))
                   .ToList();

        return new AppDbContext(optionsBuilder.Options, types);
    }
}
