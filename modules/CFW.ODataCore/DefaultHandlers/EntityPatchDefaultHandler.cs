using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.OData.Deltas;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityPatchDefaultHandler<TODataViewModel, TKey> : IEntityPatchHandler<TODataViewModel, TKey>
    where TODataViewModel : class
{
    private readonly IODataDbContextProvider _dbContextProvider;

    public EntityPatchDefaultHandler(IODataDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<Result<TODataViewModel>> Handle(TKey key, Delta<TODataViewModel> delta, CancellationToken cancellationToken)
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