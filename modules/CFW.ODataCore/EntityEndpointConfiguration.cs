//using CFW.Core.DynamicAssemblies;
//using CFW.ODataCore.EntityConfigurations;
//using CFW.ODataCore.Models;
//using CFW.ODataCore.Projectors.EFCore;
//using CFW.ODataCore.RequestHandlers;
//using Microsoft.AspNetCore.OData.Query;
//using Microsoft.EntityFrameworkCore;
//using System.Collections;
//using System.Linq.Expressions;
//using System.Reflection;
//using System.Reflection.Emit;

//namespace CFW.ODataCore;

//public abstract class EntityEndpointConfiguration
//{
//    public ApiMethod[] Methods { set; get; }

//    public string Name { get; set; }

//    public string? RoutePrefix { get; set; }

//    public Dictionary<string, Type> NestedTypes { get; set; } = new();

//    public Dictionary<string, Type> CollectionTypes { get; set; } = new();

//    public Dictionary<PropertyInfo, string> Properties { get; set; } = new();

//    protected Type BuildTypeFromNewExpression(NewExpression newExpression, string typeName)
//    {
//        var typeBuilder = DynamicTypeBuilder.CreateTypeBuilder(typeName, typeof(object));

//        foreach (var (member, argument) in newExpression.Members.Zip(newExpression.Arguments))
//        {
//            if (argument is MemberExpression memberExpression)
//            {
//                // Add simple properties
//                var propertyInfo = (PropertyInfo)memberExpression.Member;
//                var propertyType = propertyInfo.PropertyType;
//                DynamicTypeBuilder.CreateProperty(typeBuilder, member.Name, propertyType);

//                //<TSource Prop, TResult Prop>
//                Properties.Add(propertyInfo, member.Name);
//            }
//            else if (argument is NewExpression nestedExpression)
//            {
//                // Add nested types
//                var nestedType = BuildTypeFromNewExpression(nestedExpression, $"{typeName}_{member.Name}");
//                DynamicTypeBuilder.CreateProperty(typeBuilder, member.Name, nestedType);
//                NestedTypes.Add(member.Name, nestedType);
//            }
//            else if (argument is MethodCallExpression methodCallExpression)
//            {
//                var collectionNewExpr = methodCallExpression.Arguments
//                    .OfType<LambdaExpression>().Single(x => x.Body is NewExpression);
//                var newExpr = collectionNewExpr.Body as NewExpression;

//                var collectionElmType = BuildTypeFromNewExpression(newExpr, $"{typeName}_{member.Name}");

//                var collectionType = typeof(IEnumerable<>).MakeGenericType(collectionElmType);
//                DynamicTypeBuilder.CreateProperty(typeBuilder, member.Name, collectionType);
//                CollectionTypes.Add(member.Name, collectionType);
//            }
//            else
//            {
//                throw new InvalidOperationException("Invalid expression type.");
//            }
//        }

//        return typeBuilder.CreateType();
//    }

//    public LambdaExpression BuildExpressionTree(Type dbType, Type viewModelType)
//    {
//        var parameter = Expression.Parameter(dbType, "e");

//        var dbTypeProperties = dbType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
//        var bindings = new List<MemberBinding>();
//        foreach (var dbTypeProperty in dbTypeProperties)
//        {
//            var propExpr = Expression.Property(parameter, dbTypeProperty.Name);

//            if (NestedTypes.TryGetValue(dbTypeProperty.Name, out var nestedType))
//            {
//                var childProperties = nestedType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
//                var childBindings = new List<MemberBinding>();
//                foreach (var childProperty in childProperties)
//                {
//                    var mBinding = Expression.Bind(childProperty, Expression.Property(propExpr, childProperty.Name));
//                    childBindings.Add(mBinding);
//                }
//                var childInit = Expression.MemberInit(Expression.New(nestedType), childBindings);
//                bindings.Add(Expression.Bind(viewModelType.GetProperty(dbTypeProperty.Name)!, childInit));
//            }
//            else if (CollectionTypes.TryGetValue(dbTypeProperty.Name, out var collectionType))
//            {
//                // Get the target element type (e.g., ChildCollectionEntityEndpointConfgurationDbModelViewModel)
//                var viewModelElementType = collectionType.GetGenericArguments().First();

//                // Access the parent collection (e.g., e.Children)
//                var parentCollection = Expression.Property(parameter, dbTypeProperty.Name);

//                // Get the source element type of the collection (e.g., ChildEntityEndpointConfgurationDbModel)
//                var sourceElementType = parentCollection.Type.GetGenericArguments().First();

//                // Parameter for the lambda (e.g., x in x => new { x.Id, x.Name })
//                var childParameter = Expression.Parameter(sourceElementType, "x");

//                // Build bindings for the view model type (ChildCollectionEntityEndpointConfgurationDbModelViewModel)
//                var childBindings = new List<MemberBinding>();
//                foreach (var childProperty in viewModelElementType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
//                {
//                    // Access the corresponding property in the source element
//                    var sourceProperty = sourceElementType.GetProperty(childProperty.Name);
//                    if (sourceProperty != null)
//                    {
//                        var propertyAccess = Expression.Property(childParameter, sourceProperty);
//                        childBindings.Add(Expression.Bind(childProperty, propertyAccess));
//                    }
//                }

//                // Create MemberInit for the view model type
//                var childInit = Expression.MemberInit(Expression.New(viewModelElementType), childBindings);

//                // Build the lambda for Select (x => new ChildCollectionEntityEndpointConfgurationDbModelViewModel { ... })
//                var selectLambda = Expression.Lambda(childInit, childParameter);

//                // Call Enumerable.Select (e.Children.Select(x => new ...))
//                var selectCall = Expression.Call(
//                    typeof(Enumerable),
//                    "Select",
//                    new[] { sourceElementType, viewModelElementType }, // Generic arguments: TSource, TResult
//                    parentCollection,
//                    selectLambda
//                );

//                // Bind the collection to the parent view model
//                bindings.Add(Expression.Bind(viewModelType.GetProperty(dbTypeProperty.Name)!, selectCall));
//            }

//            else
//            {
//                if (!Properties.TryGetValue(dbTypeProperty, out var viewModelPropertyName))
//                    continue; // Skip properties that are not mapped in expression model

//                var viewModelProperty = viewModelType.GetProperty(viewModelPropertyName);

//                var member = Expression.Bind(viewModelProperty, propExpr);
//                bindings.Add(member);
//            }
//        }

//        var parentInit = Expression.MemberInit(Expression.New(viewModelType), bindings);
//        // Build the final lambda expression
//        return Expression.Lambda(parentInit, parameter);
//    }

//    public Type SourceType { get; protected set; }

//    public Type ViewModelType { get; protected set; }

//    public Type KeyType { get; set; }

//    public string KeyPropertyName { get; set; }

//    public Action<QueryOptions>? QueryOptionConfig { get; set; }

//    public virtual void BuildViewModelType(ModuleBuilder modelBuilder)
//    {
//        var viewModelTypeName = $"{Name.ToPascalCase()}";
//        ViewModelType = ViewModelTypeBuilder.BuildViewModelType(modelBuilder, SourceType, viewModelTypeName);
//    }
//}

//[Obsolete("End investigation")]
//public abstract class EntityConfiguration<TEntity> : EntityEndpointConfiguration
//{
//    protected void EnableQuery<TService>(Func<TService, IQueryable<TEntity>> entitySourceFactory
//        , Action<QueryOptions>? queryOptionConfig = null)
//        where TService : class
//    {
//        QueryOptionConfig = queryOptionConfig;
//        EntityQueryableFactory = s =>
//        {
//            var service = s.GetRequiredService<TService>();
//            return entitySourceFactory(service);
//        };
//    }

//    protected void EnableCreate<TService>(Func<TService, IDictionary<string, object?>, Task<Result>> createFactory)
//        where TService : class
//    {
//        EntityCreateFactory = (s, properties) =>
//        {
//            var service = s.GetRequiredService<TService>();
//            return createFactory(service, properties);
//        };
//    }

//    public Func<IServiceProvider, IQueryable<TEntity>> EntityQueryableFactory { get; private set; }

//    public Func<IServiceProvider, IDictionary<string, object?>, Task<Result>> EntityCreateFactory { get; private set; }
//}

//public class DefaultEfCoreConfiguration<TDbSet> : EntityConfiguration<TDbSet>
//    where TDbSet : class
//{
//    public override void BuildViewModelType(ModuleBuilder modelBuilder)
//    {
//        SourceType = typeof(TDbSet);
//        Properties = SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
//            .ToDictionary(x => x, x => x.Name);

//        base.BuildViewModelType(modelBuilder);

//        if (Methods.Contains(ApiMethod.Query) || Methods.Contains(ApiMethod.GetByKey))
//        {
//            EnableQuery((IODataDbContextProvider dbContextProvider) =>
//            {
//                var db = dbContextProvider.GetDbContext();
//                return db.Set<TDbSet>().AsNoTracking();
//            });
//        }

//        if (Methods.Contains(ApiMethod.Post))
//        {
//            EnableCreate(async (IODataDbContextProvider dbContextProvider, IDictionary<string, object?> properties) =>
//            {
//                var entity = Activator.CreateInstance<TDbSet>();
//                try
//                {
//                    var db = dbContextProvider.GetDbContext();

//                    var dbEntityEntry = db.Entry(entity);
//                    foreach (var (propertyName, value) in properties)
//                    {
//                        if (value is JsonDeltaSet deltaSet)
//                        {
//                            var listItemType = typeof(List<>).MakeGenericType(deltaSet.ObjectType);
//                            var listItem = (IList)Activator.CreateInstance(listItemType);

//                            foreach (var deltaItem in deltaSet.ChangedProperties)
//                            {
//                                var elementInstance = Activator.CreateInstance(deltaItem.ObjectType);
//                                var propertyEntry = db.Entry(elementInstance);
//                                foreach (var (propName, propValue) in deltaItem.ChangedProperties)
//                                {
//                                    propertyEntry.Property(propName).CurrentValue = propValue;
//                                }
//                                listItem.Add(elementInstance);
//                            }

//                            var elementDbEntityEntry = dbEntityEntry.Collection(propertyName);
//                            elementDbEntityEntry.CurrentValue = listItem;

//                            continue;
//                        }

//                        if (value is JsonDelta delta)
//                        {
//                            var nestedEntity = Activator.CreateInstance(delta.ObjectType);
//                            var navigationEntry = dbEntityEntry.Navigation(propertyName);
//                            navigationEntry.CurrentValue = nestedEntity;
//                            var nestedDbEntityEntry = db.Entry(nestedEntity);
//                            foreach (var (propName, propValue) in delta.ChangedProperties)
//                            {
//                                nestedDbEntityEntry.Property(propName).CurrentValue = propValue;
//                            }

//                            continue;
//                        }

//                        dbEntityEntry.Property(propertyName).CurrentValue = value;
//                        continue;
//                    }

//                    db.Add(entity);
//                    var affected = await db.SaveChangesAsync();

//                    if (affected == 0)
//                        return entity.Failed("Failed to create entity");

//                    return entity.Success();
//                }
//                catch (Exception ex)
//                {
//                    throw new NotImplementedException();
//                }
//            });
//        }
//    }


//}

//[Obsolete("End investigation")]
//public class QueryOptions
//{
//    // public ODataQuerySettings ODataQuerySettings { set; get; } = new ODataQuerySettings();

//    public AllowedQueryOptions AllowedQueryOptions { set; get; } = AllowedQueryOptions.All;
//}