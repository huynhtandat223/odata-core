using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Features.EntityQuery;

public interface IEntityQueryHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<Result<IQueryable>> Handle(ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken);
}
