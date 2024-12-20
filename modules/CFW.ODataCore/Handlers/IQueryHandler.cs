using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Handlers;

public interface IQueryHandler<TODataViewModel, TKey>
{
    Task<IQueryable> Query(ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken);
}
