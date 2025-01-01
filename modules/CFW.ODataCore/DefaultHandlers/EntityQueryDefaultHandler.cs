using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityQueryDefaultHandler<TODataViewModel, TDbModel>
    : EntityQueryDefaultHandler<TODataViewModel>, IEntityQueryHandler<TODataViewModel>
    where TODataViewModel : class
    where TDbModel : class
{
    private readonly Lazy<Expression<Func<TDbModel, TODataViewModel>>> _projection = new(() =>
    {
        var parameter = Expression.Parameter(typeof(TDbModel), "x");

        var destinationProperties = typeof(TODataViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var bindings = destinationProperties
            .Select(destProp =>
            {
                // Find a matching source property by name
                var sourceProp = typeof(TDbModel).GetProperty(destProp.Name, BindingFlags.Public | BindingFlags.Instance);
                if (sourceProp == null)
                    return null;

                // Ensure the types are compatible (including nullable types)
                if (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType) ||
                    (Nullable.GetUnderlyingType(destProp.PropertyType) != null &&
                     Nullable.GetUnderlyingType(destProp.PropertyType) == sourceProp.PropertyType))
                {
                    // Access the source property (x.Id, x.Name, etc.)
                    var sourcePropertyAccess = Expression.Property(parameter, sourceProp);

                    // Create a member binding for the destination property
                    return Expression.Bind(destProp, sourcePropertyAccess);
                }

                return null;
            })
            .Where(binding => binding != null)
            .ToArray();

        var newExpression = Expression.New(typeof(TODataViewModel));
        var memberInit = Expression.MemberInit(newExpression, bindings!);

        var lambda = Expression.Lambda<Func<TDbModel, TODataViewModel>>(memberInit, parameter);
        return lambda;
    });

    public EntityQueryDefaultHandler(IODataDbContextProvider dbContextProvider)
        : base(dbContextProvider)
    {
    }

    protected override IQueryable<TODataViewModel> GetQueryable(DbContext db)
    {
        var query = db.Set<TDbModel>().Select(_projection.Value).AsNoTracking();
        return query;
    }
}

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

        var query = GetQueryable(db);
        var appliedQuery = options.ApplyTo(query);
        return await Task.FromResult(appliedQuery.Success());
    }

    protected virtual IQueryable<TODataViewModel> GetQueryable(DbContext db)
    {
        return db.Set<TODataViewModel>().AsQueryable();
    }
}