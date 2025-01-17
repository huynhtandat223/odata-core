using CFW.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CFW.ODataCore.Models;

//TODO: Need to move to the project that related to EfCore
public class AutoScanModelCustomizer<TDbContext> : ModelCustomizer, IModelCustomizer
    where TDbContext : DbContext
{
    public static Type[] EntityMarkerTypes { get; set; } =
    [
        typeof(IEntity<>)
    ];

    private readonly Lazy<Type[]> _entityTypes = new(() =>
    {
        var entityTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetInterfaces()
                .Any(i => EntityMarkerTypes.Contains(i) || i.IsGenericType && EntityMarkerTypes.Contains(i.GetGenericTypeDefinition())))
            .ToArray();

        return entityTypes;
    });

    public AutoScanModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies)
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
