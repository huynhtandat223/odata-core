using Microsoft.AspNetCore.OData.Deltas;

namespace CFW.ODataCore.Features.EntityQuery;

public interface IEntityPatchHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<Result<TODataViewModel>> Handle(TKey key, Delta<TODataViewModel> delta, CancellationToken cancellationToken);
}
