using CFW.ODataCore.EFCore;
using CFW.ODataCore.OData;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Handlers;

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
        var affect = await db.Set<TODataViewModel>().Where(x => x.Id!.Equals(key))
            .ExecuteDeleteAsync(cancellationToken);

        if (affect == 0)
            return this.Failed("Can't delete entity.");

        return this.Success();
    }
}
