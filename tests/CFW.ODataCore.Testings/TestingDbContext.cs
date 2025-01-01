using CFW.ODataCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Testings;

[Entity(RoutingName, Methods = [ODataHttpMethod.Post])]
public class ConfiguredDbSet
{
    public Guid Id { set; get; }

    public string? Name { set; get; }

    public const string RoutingName = "configuredDbSets";
}

public class TestingDbContext : IdentityDbContext<IdentityUser>
{
    public TestingDbContext(DbContextOptions<TestingDbContext> options) : base(options)
    {
    }

    public DbSet<ConfiguredDbSet> ConfiguredDbSets { get; set; }
}