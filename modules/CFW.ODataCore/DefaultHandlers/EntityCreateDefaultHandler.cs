using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityCreateDefaultHandler<TODataViewModel> : IEntityCreateHandler<TODataViewModel>
    where TODataViewModel : class
{
    private readonly IODataDbContextProvider _dbContextProvider;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;

    public EntityCreateDefaultHandler(IODataDbContextProvider dbContextProvider
        , ILogger<EntityCreateDefaultHandler<TODataViewModel>> logger
        , IMapper mapper)
    {
        _dbContextProvider = dbContextProvider;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<Result<TODataViewModel>> Handle(TODataViewModel model, CancellationToken cancellationToken)
    {
        if (model is null)
            return model.Failed("Model is null.")!;

        var db = _dbContextProvider.GetContext();
        var dbSetType = typeof(TODataViewModel);

        var dbModel = typeof(TODataViewModel) == dbSetType
        ? model
            : _mapper.Map(model, dbSetType);

        if (dbModel is null)
            this.Failed("Mapping failed.");

        var entityType = db.Model.FindEntityType(dbModel!.GetType());
        if (entityType is null)
            throw new InvalidOperationException("Entity type not found.");

        Result<TODataViewModel>? result = null;
        db.ChangeTracker.TrackGraph(dbModel!, async rootEntity =>
        {
            try
            {
                await TrackGraph(db, dbModel!, rootEntity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking graph.");
                result = model.Failed(ex.Message);
            }
        });

        if (result is not null)
            return result;

        var actual = await db.SaveChangesAsync(cancellationToken);
        if (actual == 0)
            return model.Failed("No changes saved.");

        return model.Created();
    }


    private async Task TrackGraph(DbContext db
        , object dbModel, EntityEntryGraphNode rootEntity, CancellationToken cancellationToken)
    {
        if (rootEntity.Entry.Entity != dbModel)
            return; //only track root entity.

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
                    navigation.TargetEntry!.State = EntityState.Detached;
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
