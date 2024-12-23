namespace CFW.ODataCore.Features.EntityCreate;

public interface IEntityCreateHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<Result<TODataViewModel>> Handle(TODataViewModel model, CancellationToken cancellationToken);
}
