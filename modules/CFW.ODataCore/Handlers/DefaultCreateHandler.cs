using CFW.ODataCore.EFCore;
using CFW.ODataCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CFW.ODataCore.Handlers;

public interface ICreateHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<Result<TODataViewModel>> Create(TODataViewModel model, CancellationToken cancellationToken);
}

public class DefaultCreateHandler<TODataViewModel, TKey> : ICreateHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    private readonly ILogger<DefaultCreateHandler<TODataViewModel, TKey>> _logger;

    public DefaultCreateHandler(IODataDbContextProvider dbContextProvider, ILogger<DefaultCreateHandler<TODataViewModel, TKey>> logger)
    {
        _dbContextProvider = dbContextProvider;
        _logger = logger;
    }

    public async Task<Result<TODataViewModel>> Create(TODataViewModel model, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();
        db.ChangeTracker.TrackGraph(model, async rootEntity =>
        {
            try
            {
                await TrackGraph(db, model, rootEntity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking graph.");
            }
        });

        await db.SaveChangesAsync(cancellationToken);

        return model.Created();
    }

    private async Task TrackGraph(DbContext db, TODataViewModel model, EntityEntryGraphNode rootEntity, CancellationToken cancellationToken)
    {
        if (rootEntity.Entry.Entity != model)
            return;

        rootEntity.Entry.State = EntityState.Added;

        var navigations = rootEntity.Entry.Navigations
            .OfType<ReferenceEntry>()
            .ToList();

        foreach (var navigation in navigations)
        {
            var targetEntity = navigation.TargetEntry;
            if (targetEntity is null)
                throw new NotImplementedException();

            var keyProperty = targetEntity.Metadata.FindPrimaryKey()?.Properties.SingleOrDefault();
            if (keyProperty is null)
                throw new InvalidOperationException("Primary key not found.");

            var keyValue = targetEntity.Property(keyProperty.Name).CurrentValue!;
            var defaultKey = Activator.CreateInstance(keyProperty.ClrType);

            // If the key is default, then the entity is new.
            if (keyValue.ToString()!.Equals(defaultKey?.ToString()))
                navigation.TargetEntry!.State = EntityState.Added;

            // If the key is not default, then check db
            else
            {
                var dbEntity = await db.FindAsync(targetEntity.Metadata.ClrType, keyValues: [keyValue], cancellationToken);
                if (dbEntity is null)
                    navigation.TargetEntry!.State = EntityState.Added;
                else
                    navigation.TargetEntry!.State = EntityState.Unchanged;
            }

        }

        var collections = rootEntity.Entry.Collections
            .OfType<CollectionEntry>()
            .ToList();

        foreach (var collection in collections)
        {
            var targetEntities = collection.CurrentValue;
            if (targetEntities is null)
                throw new NotImplementedException();

            foreach (var targetEntity in targetEntities)
            {
                var entry = db.Entry(targetEntity);
                var keyProperty = entry.Metadata.FindPrimaryKey()?.Properties.SingleOrDefault();
                if (keyProperty is null)
                    throw new InvalidOperationException("Primary key not found.");

                var keyValue = entry.Property(keyProperty.Name).CurrentValue!;

                var defaultKey = Activator.CreateInstance(keyProperty.ClrType);

                // If the key is default, then the entity is new.
                if (keyValue.ToString()!.Equals(defaultKey?.ToString()))
                    entry.State = EntityState.Added;

                // If the key is not default, then check db
                else
                {
                    var dbEntity = await db.FindAsync(entry.Metadata.ClrType, keyValues: [keyValue], cancellationToken);
                    if (dbEntity is null)
                        entry.State = EntityState.Added;
                    else
                        entry.State = EntityState.Unchanged;
                }
            }
        }
    }
}