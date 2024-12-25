using CFW.ODataCore.Core.Metadata;
using CFW.ODataCore.Features.EFCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CFW.ODataCore.Features.EntityCreate;

public class EntityCreateDefaultHandler<TODataViewModel, TKey> : IEntityCreateHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    private readonly ILogger _logger;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly EntitySetMetadata _metadataEntity;
    private readonly IMapper _mapper;

    public EntityCreateDefaultHandler(IODataDbContextProvider dbContextProvider
        , ILogger<EntityCreateDefaultHandler<TODataViewModel, TKey>> logger
        , IActionContextAccessor actionContextAccessor
        , IMapper mapper
        , EntitySetMetadata metadataEntity)
    {
        _dbContextProvider = dbContextProvider;
        _logger = logger;
        _actionContextAccessor = actionContextAccessor;
        _mapper = mapper;
        _metadataEntity = metadataEntity;
    }

    public async Task<Result<TODataViewModel>> Handle(TODataViewModel model, CancellationToken cancellationToken)
    {
        if (_actionContextAccessor.ActionContext is not null && _actionContextAccessor.ActionContext.ModelState is not null)
        {
            var modelState = _actionContextAccessor.ActionContext.ModelState;
            if (!modelState.IsValid)
                return model.Failed(modelState.ToJsonString());
        }

        if (model is null)
            return model.Failed("Model is null.")!;

        var db = _dbContextProvider.GetContext();
        var dbSetType = _metadataEntity.DbSetType ?? typeof(TODataViewModel);

        var dbModel = typeof(TODataViewModel) == dbSetType
            ? model
            : _mapper.Map(model, dbSetType);

        if (dbModel is null)
            this.Failed("Mapping failed.");

        var entityType = db.Model.FindEntityType(dbModel!.GetType());
        if (entityType is null)
            throw new InvalidOperationException("Entity type not found.");

        db.ChangeTracker.TrackGraph(dbModel!, async rootEntity =>
        {
            try
            {
                await TrackGraph(db, dbModel!, rootEntity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking graph.");
            }
        });

        await db.SaveChangesAsync(cancellationToken);

        return model.Created();
    }

    private async Task TrackGraph(DbContext db
        , object dbModel, EntityEntryGraphNode rootEntity, CancellationToken cancellationToken)
    {
        if (rootEntity.Entry.Entity != dbModel)
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
