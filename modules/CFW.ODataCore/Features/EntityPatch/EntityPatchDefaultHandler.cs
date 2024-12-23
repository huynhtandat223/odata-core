using CFW.ODataCore.Features.EFCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData.Deltas;

namespace CFW.ODataCore.Features.EntityQuery;

public class EntityPatchDefaultHandler<TODataViewModel, TKey> : IEntityPatchHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    private readonly IActionContextAccessor _actionContextAccessor;

    public EntityPatchDefaultHandler(IODataDbContextProvider dbContextProvider, IActionContextAccessor actionContextAccessor)
    {
        _dbContextProvider = dbContextProvider;
        _actionContextAccessor = actionContextAccessor;
    }

    public async Task<Result<TODataViewModel>> Handle(TKey key, Delta<TODataViewModel> delta, CancellationToken cancellationToken)
    {
        if (_actionContextAccessor.ActionContext is not null && _actionContextAccessor.ActionContext.ModelState is not null)
        {
            var modelState = _actionContextAccessor.ActionContext.ModelState;
            if (!modelState.IsValid)
                return default(TODataViewModel).Failed(modelState.ToJsonString());
        }

        var db = _dbContextProvider.GetContext();
        var entity = await db.Set<TODataViewModel>().FindAsync(key);

        if (entity == null)
            throw new InvalidOperationException($"Entity with key {key} not found.");

        delta.Patch(entity);
        await db.SaveChangesAsync(cancellationToken);

        return entity.Success();
    }
}
