using CFW.ODataCore.EFCore;
using CFW.ODataCore.OData;
using Microsoft.AspNetCore.OData.Deltas;

namespace CFW.ODataCore.Handlers;

public interface IPatchHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<Result<TODataViewModel>> Patch(TKey key, Delta<TODataViewModel> delta, CancellationToken cancellationToken);
}
public class DefaultPatchHandler<TODataViewModel, TKey> : IPatchHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    public DefaultPatchHandler(IODataDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<Result<TODataViewModel>> Patch(TKey key, Delta<TODataViewModel> delta, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();
        var entity = await db.Set<TODataViewModel>().FindAsync(key);

        if (entity == null)
            throw new InvalidOperationException($"Entity with key {key} not found.");

        delta.Patch(entity);
        await db.SaveChangesAsync(cancellationToken);

        return entity.Success();
    }
}
