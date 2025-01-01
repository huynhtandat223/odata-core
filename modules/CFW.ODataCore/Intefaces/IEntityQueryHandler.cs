using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Intefaces;

public interface IEntityQueryHandler<TODataViewModel>
    where TODataViewModel : class
{
    Task<Result<IQueryable>> Handle(ODataQueryOptions<TODataViewModel> options
        , CancellationToken cancellationToken);
}
