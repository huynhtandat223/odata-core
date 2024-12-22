using CFW.ODataCore.EFCore;

namespace CFW.ODataCore.Features.EntitySets.Handlers;

public interface IDeleteHandler<TODataViewModel, TKey>
{
    Task<Result> Delete(TKey key, CancellationToken cancellationToken);
}

public class DefaultDeleteHandler<TODataViewModel, TKey> : IDeleteHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    public DefaultDeleteHandler(IODataDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }
    public async Task<Result> Delete(TKey key, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();
        var entity = await db.FindAsync<TODataViewModel>([key], cancellationToken);

        if (entity is null)
            return entity.Notfound();

        db.Set<TODataViewModel>().Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        return entity.Success();
    }
}
