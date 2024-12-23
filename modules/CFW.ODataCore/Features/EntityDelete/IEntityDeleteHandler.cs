namespace CFW.ODataCore.Features.EntityQuery;

public interface IEntityDeleteHandler<TODataViewModel, TKey>
    where TODataViewModel : IODataViewModel<TKey>
{
    Task<Result> Handle(TKey key, CancellationToken cancellationToken);
}
