using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Intefaces;

public interface IEntityGetByKeyHandler<TODataViewModel, TKey>
    where TODataViewModel : class
{
    Task<Result<dynamic>> Handle(TKey key, ODataQueryOptions<TODataViewModel> options
        , CancellationToken cancellationToken);

}