using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Testings;

public static class EfCoreUtils
{
    public static async Task<object?> LoadAsync(this DbContext db, Type entityType, object[] keyValues
        , CancellationToken cancellationToken = default)
    {
        var entity = await db.FindAsync(entityType, keyValues: keyValues, cancellationToken);

        if (entity is null)
            return entity;

        var entry = db.Entry(entity);

        foreach (var navigation in entry.Navigations)
        {
            await navigation.LoadAsync(cancellationToken);
        }

        foreach (var collection in entry.Collections)
        {
            await collection.LoadAsync(cancellationToken);
        }

        return entity;
    }
}
