using CFW.ODataCore.Attributes;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.OData.Deltas;
using System.Reflection;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityPatchDefaultHandler<TODataViewModel, TDbModel, TKey>
    : IEntityPatchHandler<TODataViewModel, TKey>
    where TODataViewModel : class
    where TDbModel : class
{
    private readonly IODataDbContextProvider _dbContextProvider;

    public EntityPatchDefaultHandler(IODataDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<Result> Handle(TKey key, Delta<TDbModel> delta, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetDbContext();
        var entity = await db.Set<TDbModel>().FindAsync(key);

        if (entity == null)
            throw new InvalidOperationException($"Entity with key {key} not found.");

        //TODO: More efficient way to update entity
        delta.TrySetPropertyValue("Id", key);
        delta.Patch(entity);

        var affected = await db.SaveChangesAsync(cancellationToken);

        if (affected == 0)
            return entity.Failed("No entity was updated.");

        return entity.Success();
    }

    public async Task<Result> Handle(TKey key, Delta<TODataViewModel> delta, CancellationToken cancellationToken)
    {
        if (delta.GetDeltaNestedNavigationProperties().Any())
            return this.Failed("Nested navigation properties are not supported.");

        if (delta is Delta<TDbModel> dbDelta)
            return await Handle(key, dbDelta, cancellationToken);

        var db = _dbContextProvider.GetDbContext();
        var entity = await db.Set<TDbModel>().FindAsync(key);

        if (entity == null)
            return this.Notfound();

        //TODO: More efficient way to update entity
        delta.TrySetPropertyValue("Id", key);
        foreach (var property in delta.GetChangedPropertyNames())
        {
            var actualProperty = property;
            var propInfo = typeof(TODataViewModel).GetProperty(property);
            if (propInfo == null)
                return this.Failed($"Property {property} not found.");

            var entityPropertyNameAttr = propInfo.GetCustomAttribute<EntityPropertyNameAttribute>();
            if (entityPropertyNameAttr != null)
                actualProperty = entityPropertyNameAttr.DbModelPropertyName;

            if (delta.TryGetPropertyValue(property, out var val))
                db.Entry(entity).Property(actualProperty).CurrentValue = val;
            else
                return this.Failed($"Property {property} not found.");
        }

        var affected = await db.SaveChangesAsync(cancellationToken);

        if (affected == 0)
            return entity.Failed("No entity was updated.");

        return entity.Success();
    }
}