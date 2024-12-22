using CFW.ODataCore.EFCore;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CFW.ODataCore.Features.EntitySets.Handlers;

public interface IGetByKeyHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<Result<dynamic>> Get(TKey key, ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken);
}

public class DefaultGetByKeyHandler<TODataViewModel, TKey> : IGetByKeyHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    public DefaultGetByKeyHandler(IODataDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }
    public async Task<Result<dynamic>> Get(TKey key, ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();
        var query = db.Set<TODataViewModel>().Where(x => x.Id!.Equals(key));
        var appliedQuery = options.ApplyTo(query);

        var result = await appliedQuery.Cast<dynamic>().SingleOrDefaultAsync(cancellationToken);

        if (result == null)
            return new Result<dynamic>
            {
                HttpStatusCode = HttpStatusCode.NotFound,
                IsSuccess = false,
            };

        return new Result<dynamic>
        {
            HttpStatusCode = HttpStatusCode.OK,
            IsSuccess = true,
            Data = result,
        };
    }
}
