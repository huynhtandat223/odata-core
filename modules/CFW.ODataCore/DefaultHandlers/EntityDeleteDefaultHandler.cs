using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Projectors.EFCore;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityDeleteDefaultHandler<TODataViewModel, TKey> : IEntityDeleteHandler<TODataViewModel, TKey>
    where TODataViewModel : class
{
    private readonly IODataDbContextProvider _dbContextProvider;
    public EntityDeleteDefaultHandler(IODataDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }
    public async Task<Result> Handle(TKey key, CancellationToken cancellationToken)
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
