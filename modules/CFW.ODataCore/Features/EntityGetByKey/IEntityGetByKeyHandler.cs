using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Features.EntityQuery;

public interface IEntityGetByKeyHandler<TODataViewModel, TKey>
    where TODataViewModel : IODataViewModel<TKey>
{
    Task<Result<dynamic>> Handle(TKey key, ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken);
}
