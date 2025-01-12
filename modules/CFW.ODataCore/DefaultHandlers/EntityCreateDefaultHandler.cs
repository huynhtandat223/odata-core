using CFW.ODataCore.Attributes;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;
using System.Reflection;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityCreateDefaultHandler<TODataViewModel, TDbModel> : IEntityCreateHandler<TODataViewModel>
    where TODataViewModel : class
{
    private readonly IODataDbContextProvider _dbContextProvider;
    private readonly ILogger _logger;

    public EntityCreateDefaultHandler(IODataDbContextProvider dbContextProvider
        , ILogger<EntityCreateDefaultHandler<TODataViewModel, TDbModel>> logger)
    {
        _dbContextProvider = dbContextProvider;
        _logger = logger;
    }

    public async Task<Result<TODataViewModel>> Handle(TODataViewModel model, CancellationToken cancellationToken)
    {
        var db = _dbContextProvider.GetContext();

        Result<TODataViewModel>? result = null;
        var dbModel = Map(model);
        db.ChangeTracker.TrackGraph(dbModel!, async rootEntity =>
        {
            try
            {
                await TrackGraph(db, dbModel!, rootEntity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking graph.");
                result = model.Failed(ex.Message);
            }
        });

        if (result is not null)
            return result;

        var actual = await db.SaveChangesAsync(cancellationToken);
        if (actual == 0)
            return model.Failed("No changes saved.");

        return model.Created();
    }

    private TDbModel Map(TODataViewModel viewModel)
    {
        if (viewModel is TDbModel dbModel)
            return dbModel;

        var projection = _reverseProjection.Value;
        dbModel = projection.Compile()(viewModel);

        return dbModel;
    }

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

    private readonly Lazy<Expression<Func<TODataViewModel, TDbModel>>> _reverseProjection = new(() =>
    {
        var parameter = Expression.Parameter(typeof(TODataViewModel), "x");

        var destinationProperties = typeof(TDbModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var bindings = destinationProperties
            .Select(destProp =>
            {
                // Check if there's a matching ViewModel property by attribute or name
                var viewModelProp = typeof(TODataViewModel)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(vmProp =>
                    {
                        var attr = vmProp.GetCustomAttribute<EntityPropertyNameAttribute>();
                        return (attr != null && attr.DbModelPropertyName == destProp.Name) ||
                               vmProp.Name == destProp.Name;
                    });

                if (viewModelProp == null)
                    return null;

                var sourcePropertyAccess = Expression.Property(parameter, viewModelProp);

                // Handle collections
                if (destProp.PropertyType.IsCommonGenericCollectionType()
                    && viewModelProp.PropertyType.IsCommonGenericCollectionType())
                {
                    var sourceElementType = GetCollectionElementType(viewModelProp.PropertyType);
                    var destinationElementType = GetCollectionElementType(destProp.PropertyType);

                    if (sourceElementType != null && destinationElementType != null)
                    {
                        var nestedProjection = CreateNestedProjection(sourceElementType, destinationElementType);
                        if (nestedProjection == null)
                            return null;

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

                        // Add null check for collection
                        var nullCheck = Expression.Condition(
                            Expression.Equal(sourcePropertyAccess, Expression.Constant(null)),
                            Expression.Constant(null, destProp.PropertyType),
                            toListCall
                        );

                        return Expression.Bind(destProp, nullCheck);
                    }
                }

                // Handle nested complex types
                if (!destProp.PropertyType.IsValueType && !destProp.PropertyType.IsPrimitive && destProp.PropertyType != typeof(string))
                {
                    var nestedProjection = CreateNestedProjection(viewModelProp.PropertyType, destProp.PropertyType);
                    if (nestedProjection == null)
                        return null;

                    var nestedExpression = Expression.Invoke(nestedProjection, sourcePropertyAccess);

                    // Add null check for complex properties
                    var nullCheck = Expression.Condition(
                        Expression.Equal(sourcePropertyAccess, Expression.Constant(null)),
                        Expression.Constant(null, destProp.PropertyType),
                        nestedExpression
                    );

                    return Expression.Bind(destProp, nullCheck);
                }

                // Handle simple properties
                if (destProp.PropertyType.IsAssignableFrom(viewModelProp.PropertyType) ||
                    (Nullable.GetUnderlyingType(destProp.PropertyType) != null &&
                     Nullable.GetUnderlyingType(destProp.PropertyType) == viewModelProp.PropertyType))
                {
                    return Expression.Bind(destProp, sourcePropertyAccess);
                }

                return null;
            })
            .Where(binding => binding != null)
            .ToArray();

        var newExpression = Expression.New(typeof(TDbModel));
        var memberInit = Expression.MemberInit(newExpression, bindings!);

        var lambda = Expression.Lambda<Func<TODataViewModel, TDbModel>>(memberInit, parameter);
        return lambda;
    });

    private async Task TrackGraph(DbContext db
        , object dbModel, EntityEntryGraphNode rootEntity, CancellationToken cancellationToken)
    {
        if (rootEntity.Entry.Entity != dbModel)
            return; //only track root entity.

        rootEntity.Entry.State = EntityState.Added;

        var navigations = rootEntity.Entry.Navigations
            .OfType<ReferenceEntry>()
            .ToList();

        foreach (var navigation in navigations)
        {
            var targetEntity = navigation.TargetEntry;
            if (targetEntity is null)
                continue;

            var keyProperty = targetEntity.Metadata.FindPrimaryKey()?.Properties.SingleOrDefault();
            if (keyProperty is null)
                throw new InvalidOperationException("Primary key not found.");

            var keyValue = targetEntity.Property(keyProperty.Name).CurrentValue!;
            var defaultKey = Activator.CreateInstance(keyProperty.ClrType);

            // If the key is default, then the entity is new.
            if (keyValue.ToString()!.Equals(defaultKey?.ToString()))
                navigation.TargetEntry!.State = EntityState.Added;

            // If the key is not default, then check db
            else
            {
                var dbEntity = await db.FindAsync(targetEntity.Metadata.ClrType, keyValues: [keyValue], cancellationToken);
                if (dbEntity is null)
                    navigation.TargetEntry!.State = EntityState.Added;
                else
                    navigation.TargetEntry!.State = EntityState.Detached;
            }

        }

        var collections = rootEntity.Entry.Collections
            .OfType<CollectionEntry>()
            .ToList();

        foreach (var collection in collections)
        {
            var targetEntities = collection.CurrentValue;
            if (targetEntities is null)
                continue;

            foreach (var targetEntity in targetEntities)
            {
                var entry = db.Entry(targetEntity);
                var keyProperty = entry.Metadata.FindPrimaryKey()?.Properties.SingleOrDefault();
                if (keyProperty is null)
                    throw new InvalidOperationException("Primary key not found.");

                var keyValue = entry.Property(keyProperty.Name).CurrentValue!;

                var defaultKey = Activator.CreateInstance(keyProperty.ClrType);

                // If the key is default, then the entity is new.
                if (keyValue.ToString()!.Equals(defaultKey?.ToString()))
                    entry.State = EntityState.Added;
                // If the key is not default, then check db
                else
                {
                    var dbEntity = await db.FindAsync(entry.Metadata.ClrType, keyValues: [keyValue], cancellationToken);
                    if (dbEntity is null)
                        entry.State = EntityState.Added;
                    else
                        entry.State = EntityState.Unchanged;
                }
            }
        }
    }
}
