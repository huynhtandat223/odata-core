using System.Linq.Expressions;
using System.Reflection;

namespace CFW.Core.Builders;
public static class ExpressionTreeBuilder
{
    /// <summary>
    /// Builds an expression tree to map properties from the source type to the ViewModel type.
    /// </summary>
    public static Expression<Func<TSource, TTarget>> BuildMappingExpression<TSource, TTarget>()
    {
        // Parameter for the source object (e.g., "source")
        var sourceParameter = Expression.Parameter(typeof(TSource), "source");

        // List to hold member bindings
        var bindings = new List<MemberBinding>();

        // Iterate through properties of the source type
        foreach (var sourceProperty in typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Find the matching property in the target type
            var targetProperty = typeof(TTarget).GetProperty(sourceProperty.Name, BindingFlags.Public | BindingFlags.Instance);

            if (targetProperty != null && targetProperty.CanWrite)
            {
                // Create a property access for the source property
                var sourcePropertyAccess = Expression.Property(sourceParameter, sourceProperty);

                // Bind the source property to the target property
                bindings.Add(Expression.Bind(targetProperty, sourcePropertyAccess));
            }
        }

        // Create the initialization of the target type
        var body = Expression.MemberInit(Expression.New(typeof(TTarget)), bindings);

        // Return the lambda expression
        return Expression.Lambda<Func<TSource, TTarget>>(body, sourceParameter);
    }
}

