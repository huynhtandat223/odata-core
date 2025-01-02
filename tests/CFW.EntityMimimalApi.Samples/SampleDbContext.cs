using Microsoft.EntityFrameworkCore;

namespace CFW.EntityMimimalApi.Samples;

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
    {
    }
}
