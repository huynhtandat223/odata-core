using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Models;

public interface IDbContextProvider
{
    DbContext GetDbContext();
}

public class ContextProvider<TDbContext> : IDbContextProvider
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    public ContextProvider(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public DbContext GetDbContext()
    {
        return _dbContext;
    }
}
