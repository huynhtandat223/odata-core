namespace CFW.ODataCore.Intefaces;

public interface IEntityCreateHandler<TODataViewModel>
    where TODataViewModel : class
{
    Task<Result<TODataViewModel>> Handle(TODataViewModel entity, CancellationToken cancellationToken);
}
