using CFW.ODataCore.Models.Requests;

namespace CFW.ODataCore.Intefaces;

public interface IEntityCreationHandler<TEntity>
{
    Task<Result> Handle(CreationCommand<TEntity> command, CancellationToken cancellationToken);
}