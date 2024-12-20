using CFW.ODataCore.Core;
using CFW.ODataCore.EFCore;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Handlers;

public class ApiHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    private readonly IServiceProvider _serviceProvider;

    public ApiHandler(IODataDbContextProvider dbContextProvider, IServiceProvider serviceProvider)
    {
        _dbContextProvider = dbContextProvider;
        _serviceProvider = serviceProvider;
    }

    public Task<IQueryable> Query(ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        var queryHandler = _serviceProvider.GetService<IQueryHandler<TODataViewModel, TKey>>();
        var db = _dbContextProvider.GetContext();
        if (queryHandler is not null)
            return queryHandler.Query(options, cancellationToken);

        var query = db.Set<TODataViewModel>();
        var appliedQuery = options.ApplyTo(query);

        return Task.FromResult(appliedQuery);
    }

    public async Task<dynamic?> Get(TKey? id, ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();
        var query = db.Set<TODataViewModel>().Where(x => x.Id!.Equals(id));
        var appliedQuery = options.ApplyTo(query);

        var result = await appliedQuery.Cast<dynamic>().SingleOrDefaultAsync(cancellationToken);

        return result;
    }

    public async Task<TODataViewModel> Patch(TKey id, Delta<TODataViewModel> delta, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();
        var entity = await db.Set<TODataViewModel>().FindAsync(id);

        if (entity == null)
            throw new InvalidOperationException($"Entity with id {id} not found.");

        delta.Patch(entity);
        await db.SaveChangesAsync(cancellationToken);

        return entity;
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
