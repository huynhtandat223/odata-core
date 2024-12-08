using CFW.Core.Entities;
using CFW.ODataCore.Core;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Handlers;

public class ApiHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly AppDbContext _db;

    public ApiHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TODataViewModel> Create(TODataViewModel model, CancellationToken cancellationToken)
    {
        if (model is IEntity<TKey>)
        {
            _db.Set<TODataViewModel>().Add(model);
            await _db.SaveChangesAsync(cancellationToken);

            return model;
        }

        throw new NotImplementedException();
    }

    public Task<IQueryable> Query(ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        var query = _db.Set<TODataViewModel>();
        var appliedQuery = options.ApplyTo(query);

        return Task.FromResult(appliedQuery);
    }

    public async Task<dynamic?> Get(TKey? id, ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        var query = _db.Set<TODataViewModel>().Where(x => x.Id!.Equals(id));
        var appliedQuery = options.ApplyTo(query);

        var result = await appliedQuery.Cast<dynamic>().SingleOrDefaultAsync(cancellationToken);

        return result;
    }

    public async Task<TODataViewModel> Patch(TKey id, Delta<TODataViewModel> delta, CancellationToken cancellationToken)
    {
        var entity = await _db.Set<TODataViewModel>().FindAsync(id);

        if (entity == null)
            throw new InvalidOperationException($"Entity with id {id} not found.");

        delta.Patch(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task Delete(TKey key, CancellationToken cancellationToken)
    {
        await _db.Set<TODataViewModel>().Where(x => x.Id!.Equals(key))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
