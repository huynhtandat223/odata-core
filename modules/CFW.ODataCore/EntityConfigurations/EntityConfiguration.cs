using System.Linq.Expressions;
using System.Reflection;

namespace CFW.ODataCore.EntityConfigurations;

[Obsolete]
public abstract class BaseEntityConfiguration<TEntity>
{
    public BaseEntityConfiguration()
    {
        if (Model.Body is not NewExpression newExpression)
            throw new InvalidOperationException("Expression must be a 'new' expression.");

        ViewModelType = BuildTypeFromNewExpression(newExpression, $"{Name}ViewModel");

        var sourceType = typeof(TEntity);
        LambdaExpression = BuildExpressionTree(typeof(TEntity), ViewModelType);
    }

    public Dictionary<string, Type> NestedTypes { get; } = new();

    public Dictionary<string, Type> CollectionTypes { get; } = new();

    public Dictionary<PropertyInfo, string> Properties { get; } = new();

    private Type BuildTypeFromNewExpression(NewExpression newExpression, string typeName)
    {
        var typeBuilder = DynamicTypeBuilder.CreateTypeBuilder(typeName, typeof(object));

        foreach (var (member, argument) in newExpression.Members.Zip(newExpression.Arguments))
        {
            if (argument is MemberExpression memberExpression)
            {
                // Add simple properties
                var propertyInfo = (PropertyInfo)memberExpression.Member;
                var propertyType = propertyInfo.PropertyType;
                DynamicTypeBuilder.CreateProperty(typeBuilder, member.Name, propertyType);

                //<TSource Prop, TResult Prop>
                Properties.Add(propertyInfo, member.Name);
            }
            else if (argument is NewExpression nestedExpression)
            {
                // Add nested types
                var nestedType = BuildTypeFromNewExpression(nestedExpression, $"{typeName}_{member.Name}");
                DynamicTypeBuilder.CreateProperty(typeBuilder, member.Name, nestedType);
                NestedTypes.Add(member.Name, nestedType);
            }
            else if (argument is MethodCallExpression methodCallExpression)
            {
                var collectionNewExpr = methodCallExpression.Arguments
                    .OfType<LambdaExpression>().Single(x => x.Body is NewExpression);
                var newExpr = collectionNewExpr.Body as NewExpression;

                var collectionElmType = BuildTypeFromNewExpression(newExpr, $"{typeName}_{member.Name}");

                var collectionType = typeof(IEnumerable<>).MakeGenericType(collectionElmType);
                DynamicTypeBuilder.CreateProperty(typeBuilder, member.Name, collectionType);
                CollectionTypes.Add(member.Name, collectionType);
            }
            else
            {
                throw new InvalidOperationException("Invalid expression type.");
            }
        }

        return typeBuilder.CreateType();
    }

    public LambdaExpression BuildExpressionTree(Type dbType, Type viewModelType)
    {
        var parameter = Expression.Parameter(dbType, "e");

        var dbTypeProperties = dbType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var bindings = new List<MemberBinding>();
        foreach (var dbTypeProperty in dbTypeProperties)
        {
            var propExpr = Expression.Property(parameter, dbTypeProperty.Name);


            if (NestedTypes.TryGetValue(dbTypeProperty.Name, out var nestedType))
            {
                var childProperties = nestedType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var childBindings = new List<MemberBinding>();
                foreach (var childProperty in childProperties)
                {
                    var mBinding = Expression.Bind(childProperty, Expression.Property(propExpr, childProperty.Name));
                    childBindings.Add(mBinding);
                }
                var childInit = Expression.MemberInit(Expression.New(nestedType), childBindings);
                bindings.Add(Expression.Bind(viewModelType.GetProperty(dbTypeProperty.Name)!, childInit));
            }
            else if (CollectionTypes.TryGetValue(dbTypeProperty.Name, out var collectionType))
            {
                // Get the target element type (e.g., ChildCollectionEntityEndpointConfgurationDbModelViewModel)
                var viewModelElementType = collectionType.GetGenericArguments().First();

                // Access the parent collection (e.g., e.Children)
                var parentCollection = Expression.Property(parameter, dbTypeProperty.Name);

                // Get the source element type of the collection (e.g., ChildEntityEndpointConfgurationDbModel)
                var sourceElementType = parentCollection.Type.GetGenericArguments().First();

                // Parameter for the lambda (e.g., x in x => new { x.Id, x.Name })
                var childParameter = Expression.Parameter(sourceElementType, "x");

                // Build bindings for the view model type (ChildCollectionEntityEndpointConfgurationDbModelViewModel)
                var childBindings = new List<MemberBinding>();
                foreach (var childProperty in viewModelElementType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Access the corresponding property in the source element
                    var sourceProperty = sourceElementType.GetProperty(childProperty.Name);
                    if (sourceProperty != null)
                    {
                        var propertyAccess = Expression.Property(childParameter, sourceProperty);
                        childBindings.Add(Expression.Bind(childProperty, propertyAccess));
                    }
                }

                // Create MemberInit for the view model type
                var childInit = Expression.MemberInit(Expression.New(viewModelElementType), childBindings);

                // Build the lambda for Select (x => new ChildCollectionEntityEndpointConfgurationDbModelViewModel { ... })
                var selectLambda = Expression.Lambda(childInit, childParameter);

                // Call Enumerable.Select (e.Children.Select(x => new ...))
                var selectCall = Expression.Call(
                    typeof(Enumerable),
                    "Select",
                    new[] { sourceElementType, viewModelElementType }, // Generic arguments: TSource, TResult
                    parentCollection,
                    selectLambda
                );

                // Bind the collection to the parent view model
                bindings.Add(Expression.Bind(viewModelType.GetProperty(dbTypeProperty.Name)!, selectCall));
            }

            else
            {
                if (!Properties.TryGetValue(dbTypeProperty, out var viewModelPropertyName))
                    continue; // Skip properties that are not mapped in expression model

                var viewModelProperty = viewModelType.GetProperty(viewModelPropertyName);

                var member = Expression.Bind(viewModelProperty, propExpr);
                bindings.Add(member);
            }
        }

        var parentInit = Expression.MemberInit(Expression.New(viewModelType), bindings);
        // Build the final lambda expression
        return Expression.Lambda(parentInit, parameter);
    }

    public virtual string Name => typeof(TEntity).Name;

    public abstract Expression<Func<TEntity, object>> Model { get; }

    public LambdaExpression LambdaExpression { get; }

    public Type ViewModelType { get; set; }
}
