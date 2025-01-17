using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityCreationHandler<TEntity> : IEntityCreationHandler<TEntity>
{
    private readonly IDbContextProvider _dbContextProvider;

    public EntityCreationHandler(IDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<Result> Handle(TEntity entity, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetDbContext();
        db.Add(entity!);
        await db.SaveChangesAsync(cancellationToken);

        return entity.Created();
    }
}