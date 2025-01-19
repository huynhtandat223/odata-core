using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityCreationHandler<TEntity> : IEntityCreationHandler<TEntity>
    where TEntity : class, new()
{
    private readonly IDbContextProvider _dbContextProvider;

    public EntityCreationHandler(IDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<Result> Handle(CreationCommand<TEntity> command, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetDbContext();

        var newEntity = new TEntity();
        var entry = db.Entry(newEntity);
        entry.State = EntityState.Added;
        foreach (var property in command.Delta.ChangedProperties)
        {
            entry.Property(property.Key).CurrentValue = property.Value;
        }

        var affected = await db.SaveChangesAsync(cancellationToken);
        if (affected == 0)
        {
            return newEntity.Failed("Failed to create entity");
        }

        return newEntity.Created();
    }
}