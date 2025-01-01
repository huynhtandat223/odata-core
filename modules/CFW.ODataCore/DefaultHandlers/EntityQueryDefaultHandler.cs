using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityQueryDefaultHandler<TODataViewModel> : IEntityQueryHandler<TODataViewModel>
    where TODataViewModel : class
{
    private readonly IODataDbContextProvider _dbContextProvider;

    public EntityQueryDefaultHandler(IODataDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<Result<IQueryable>> Handle(ODataQueryOptions<TODataViewModel> options
        , CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();
        var query = db.Set<TODataViewModel>().AsNoTracking();
        var appliedQuery = options.ApplyTo(query);

        return await Task.FromResult(appliedQuery.Success());
    }
}