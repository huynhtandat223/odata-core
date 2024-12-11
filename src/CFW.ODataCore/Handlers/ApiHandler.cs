using CFW.ODataCore.Core;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Handlers;

public interface IQueryHandler<TODataViewModel, TKey>
{
    Task<IQueryable> Query(ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken);
}

public class ApiHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly AppDbContext _db;
    private readonly IServiceProvider _serviceProvider;

    public ApiHandler(AppDbContext db, IServiceProvider serviceProvider)
    {
        _db = db;
        _serviceProvider = serviceProvider;
    }

    public async Task<TODataViewModel> Create(TODataViewModel model, CancellationToken cancellationToken)
    {
        _db.ChangeTracker.TrackGraph(model, async rootEntity =>
        {
            rootEntity.Entry.State = EntityState.Added;

            var navigations = rootEntity.Entry.Navigations
                .OfType<Microsoft.EntityFrameworkCore.ChangeTracking.ReferenceEntry>()
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

                if (keyValue.ToString()!.Equals(defaultKey))
                    navigation.TargetEntry!.State = EntityState.Added;
                else
                    navigation.TargetEntry!.State = EntityState.Detached;
            }
        });

        await _db.SaveChangesAsync(cancellationToken);

        return model;
    }

    public Task<IQueryable> Query(ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        var queryHandler = _serviceProvider.GetService<IQueryHandler<TODataViewModel, TKey>>();
        if (queryHandler is not null)
            return queryHandler.Query(options, cancellationToken);

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
