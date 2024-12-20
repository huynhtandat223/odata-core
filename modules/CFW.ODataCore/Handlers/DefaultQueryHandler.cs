using CFW.ODataCore.EFCore;
using CFW.ODataCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Handlers;

public interface IQueryHandler<TODataViewModel, TKey>
{
    Task<Result<IQueryable>> Query(ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken);
}

public class DefaultQueryHandler<TODataViewModel, TKey> : IQueryHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    public DefaultQueryHandler(IODataDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public Task<Result<IQueryable>> Query(ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();
        var query = db.Set<TODataViewModel>().AsNoTracking();
        var appliedQuery = options.ApplyTo(query);

        return Task.FromResult(appliedQuery.Success());
    }

}