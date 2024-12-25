using CFW.ODataCore.Core.Metadata;
using CFW.ODataCore.Features.EFCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityQueryDefaultHandler<TODataViewModel, TKey> : IEntityQueryHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly EntitySetMetadata _metadataEntity;

    public EntityQueryDefaultHandler(IODataDbContextProvider dbContextProvider
        , IActionContextAccessor actionContextAccessor
        , EntitySetMetadata metadataEntity)
    {
        _dbContextProvider = dbContextProvider;
        _actionContextAccessor = actionContextAccessor;
        _metadataEntity = metadataEntity;
    }

    public async Task<Result<IQueryable>> Handle(ODataQueryOptions<TODataViewModel> options
        , CancellationToken cancellationToken)
    {
        if (_metadataEntity.DbSetType != typeof(TODataViewModel))
            throw new NotImplementedException();

        if (_actionContextAccessor.ActionContext is not null && _actionContextAccessor.ActionContext.ModelState is not null)
        {
            var modelState = _actionContextAccessor.ActionContext.ModelState;
            if (!modelState.IsValid)
                return default(IQueryable).Failed(modelState.ToJsonString());
        }

        var db = _dbContextProvider.GetContext();
        var query = db.Set<TODataViewModel>().AsNoTracking();
        var appliedQuery = options.ApplyTo(query);

        return await Task.FromResult(appliedQuery.Success());
    }
}
