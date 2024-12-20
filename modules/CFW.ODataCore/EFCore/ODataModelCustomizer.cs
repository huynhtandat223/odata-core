using CFW.Core.Entities;
using CFW.ODataCore.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CFW.ODataCore.EFCore;

public interface IODataDbContextProvider
{
    DbContext GetContext();
}

public class ContextProvider<TDbContext> : IODataDbContextProvider
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    public ContextProvider(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public DbContext GetContext()
    {
        return _dbContext;
    }
}

public class ODataModelCustomizer<TDbContext> : ModelCustomizer, IModelCustomizer
    where TDbContext : DbContext
{
    private readonly Lazy<Type[]> _entityTypes = new(() =>
    {
        var entityInterface = typeof(IEntity<>);
        var containers = ODataContainerCollection.Instance.MetadataContainers;
        var entityTypes = containers
            .SelectMany(x => x.EntityMetadataList)
            .Where(x => x.ViewModelType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == entityInterface))
            .Select(y => y.ViewModelType)
            .ToArray();
        return entityTypes;
    });

    public ODataModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies)
    {

    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        if (context is not TDbContext)
        {
            return;
        }

        foreach (var entityType in _entityTypes.Value)
        {
            modelBuilder.Entity(entityType);
        }
    }
}
