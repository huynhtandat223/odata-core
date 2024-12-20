using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Testings;

public class TestingDbContext : IdentityDbContext<IdentityUser>
{
    public TestingDbContext(DbContextOptions<TestingDbContext> options) : base(options)
    {
    }
}