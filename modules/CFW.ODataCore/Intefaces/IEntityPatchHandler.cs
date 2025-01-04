using Microsoft.AspNetCore.OData.Deltas;

namespace CFW.ODataCore.Intefaces;

public interface IEntityPatchHandler<TODataViewModel, TKey>
    where TODataViewModel : class
{
    Task<Result> Handle(TKey key, Delta<TODataViewModel> delta, CancellationToken cancellationToken);
}