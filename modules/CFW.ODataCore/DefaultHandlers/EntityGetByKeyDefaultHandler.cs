using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityGetByKeyDefaultHandler<TODataViewModel, TKey>
    : IEntityGetByKeyHandler<TODataViewModel, TKey>
    where TODataViewModel : class
{
    private readonly IODataDbContextProvider _dbContextProvider;

    public EntityGetByKeyDefaultHandler(IODataDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<Result<dynamic>> Handle(TKey key, ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();
        var keyName = GetKeyName<TODataViewModel>(db);

        // Dynamically build the query
        var parameter = Expression.Parameter(typeof(TODataViewModel), "x");
        var property = Expression.Property(parameter, keyName);
        var value = Expression.Constant(key);
        var equal = Expression.Equal(property, value);
        var predicate = Expression.Lambda<Func<TODataViewModel, bool>>(equal, parameter);

        var query = db.Set<TODataViewModel>().Where(predicate);


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

    public static string GetKeyName<TEntity>(DbContext dbContext)
    where TEntity : class
    {
        // Get the entity type from the DbContext
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity));
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} not found in DbContext.");

        // Get the key property name
        var key = entityType.FindPrimaryKey()?.Properties.FirstOrDefault();
        if (key == null)
            throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not have a primary key defined.");

        return key.Name;
    }
}
