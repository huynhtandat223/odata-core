namespace CFW.ODataCore.Intefaces;

public interface IEntityCreationHandler<TEntity>
{
    Task<Result> Handle(TEntity entity, CancellationToken cancellationToken);
}
