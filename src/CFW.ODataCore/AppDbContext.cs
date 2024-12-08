using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore;

public class AppDbContext : DbContext
{
    private readonly IEnumerable<Type> _entityTypes = [];

    public AppDbContext(DbContextOptions options, List<Type> entityTypes) : base(options)
    {
        _entityTypes = entityTypes;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var type in _entityTypes)
            modelBuilder.Entity(type);
    }
}
