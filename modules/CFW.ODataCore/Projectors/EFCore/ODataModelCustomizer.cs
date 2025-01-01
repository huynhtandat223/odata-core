using CFW.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CFW.ODataCore.Projectors.EFCore;

public class ODataModelCustomizer<TDbContext> : ModelCustomizer, IModelCustomizer
    where TDbContext : DbContext
{
    private readonly Lazy<Type[]> _entityTypes = new(() =>
    {
        var entityInterface = typeof(IEntity<>);
        var entityTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == entityInterface))
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
            return;

        foreach (var entityType in _entityTypes.Value)
        {
            modelBuilder.Entity(entityType);
        }
    }
}
