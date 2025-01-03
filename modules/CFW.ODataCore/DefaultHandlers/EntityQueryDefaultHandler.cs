using CFW.ODataCore.Attributes;
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
                // Check if the property has the EntityPropertyNameAttribute
                var entityPropertyNameAttr = destProp.GetCustomAttribute<EntityPropertyNameAttribute>();
                var sourcePropName = entityPropertyNameAttr?.DbModelPropertyName ?? destProp.Name;

                // Find the matching source property
                var sourceProp = typeof(TDbModel).GetProperty(sourcePropName, BindingFlags.Public | BindingFlags.Instance);
                if (sourceProp == null)
                    return null;

                // If the property is a collection, handle it
                if (destProp.PropertyType.IsCommonGenericCollectionType()
                    && sourceProp.PropertyType.IsCommonGenericCollectionType())
                {
                    var sourceElementType = GetCollectionElementType(sourceProp.PropertyType);
                    var destinationElementType = GetCollectionElementType(destProp.PropertyType);

                    if (sourceElementType != null && destinationElementType != null)
                    {
                        var nestedProjection = CreateNestedProjection(sourceElementType, destinationElementType);
                        if (nestedProjection == null)
                            return null;

                        var sourcePropertyAccess = Expression.Property(parameter, sourceProp);

                        // Call LINQ Select to transform the collection
                        var selectMethod = typeof(Enumerable)
                            .GetMethods(BindingFlags.Static | BindingFlags.Public)
                            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(sourceElementType, destinationElementType);

                        var selectCall = Expression.Call(selectMethod, sourcePropertyAccess, nestedProjection);

                        // Convert the result to the appropriate collection type (e.g., List<T>)
                        var toListMethod = typeof(Enumerable)
                            .GetMethods(BindingFlags.Static | BindingFlags.Public)
                            .First(m => m.Name == "ToList" && m.GetParameters().Length == 1)
                            .MakeGenericMethod(destinationElementType);

                        var toListCall = Expression.Call(toListMethod, selectCall);

                        return Expression.Bind(destProp, toListCall);
                    }
                }

                // Handle complex types recursively
                if (!destProp.PropertyType.IsValueType && !destProp.PropertyType.IsPrimitive && destProp.PropertyType != typeof(string))
                {
                    var nestedProjection = CreateNestedProjection(sourceProp.PropertyType, destProp.PropertyType);
                    if (nestedProjection == null)
                        return null;

                    var sourcePropertyAccess = Expression.Property(parameter, sourceProp);
                    var nestedExpression = Expression.Invoke(nestedProjection, sourcePropertyAccess);
                    return Expression.Bind(destProp, nestedExpression);
                }

                // Handle simple properties
                if (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType) ||
                    (Nullable.GetUnderlyingType(destProp.PropertyType) != null &&
                     Nullable.GetUnderlyingType(destProp.PropertyType) == sourceProp.PropertyType))
                {
                    var sourcePropertyAccess = Expression.Property(parameter, sourceProp);
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

    private static Type? GetCollectionElementType(Type type)
    {
        return type.IsGenericType ? type.GetGenericArguments()[0] : null;
    }

    private static LambdaExpression? CreateNestedProjection(Type sourceType, Type destinationType)
    {
        var parameter = Expression.Parameter(sourceType, "x");

        var destinationProperties = destinationType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var bindings = destinationProperties
            .Select(destProp =>
            {
                var entityPropertyNameAttr = destProp.GetCustomAttribute<EntityPropertyNameAttribute>();
                var sourcePropName = entityPropertyNameAttr?.DbModelPropertyName ?? destProp.Name;

                var sourceProp = sourceType.GetProperty(sourcePropName, BindingFlags.Public | BindingFlags.Instance);
                if (sourceProp == null)
                    return null;

                if (!destProp.PropertyType.IsValueType && !destProp.PropertyType.IsPrimitive && destProp.PropertyType != typeof(string))
                {
                    var nestedProjection = CreateNestedProjection(sourceProp.PropertyType, destProp.PropertyType);
                    if (nestedProjection == null)
                        return null;

                    var sourcePropertyAccess = Expression.Property(parameter, sourceProp);
                    var nestedExpression = Expression.Invoke(nestedProjection, sourcePropertyAccess);
                    return Expression.Bind(destProp, nestedExpression);
                }

                if (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType) ||
                    (Nullable.GetUnderlyingType(destProp.PropertyType) != null &&
                     Nullable.GetUnderlyingType(destProp.PropertyType) == sourceProp.PropertyType))
                {
                    var sourcePropertyAccess = Expression.Property(parameter, sourceProp);
                    return Expression.Bind(destProp, sourcePropertyAccess);
                }

                return null;
            })
            .Where(binding => binding != null)
            .ToArray();

        if (!bindings.Any())
            return null;

        var newExpression = Expression.New(destinationType);
        var memberInit = Expression.MemberInit(newExpression, bindings!);

        return Expression.Lambda(memberInit, parameter);
    }


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