namespace CFW.ODataCore.Intefaces;

public interface IEntityDeleteHandler<TODataViewModel, TKey>
    where TODataViewModel : class
{
    Task<Result> Handle(TKey key, CancellationToken cancellationToken);
}
