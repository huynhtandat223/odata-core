using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Projectors.EFCore;

public static class EfCoreUtils
{
    public static async Task<object?> LoadAsync(this DbContext db, Type entityType, object[] keyValues
        , IEnumerable<string> navigations, IEnumerable<string> collections, CancellationToken cancellationToken = default)
    {
        var entity = await db.FindAsync(entityType, keyValues: keyValues, cancellationToken);

        if (entity is null)
            return entity;

        var entry = db.Entry(entity);
        foreach (var navigation in navigations)
        {
            entry.Reference(navigation).Load();
        }

        foreach (var collection in collections)
        {
            entry.Collection(collection).Load();
        }

        return entity;
    }
}
