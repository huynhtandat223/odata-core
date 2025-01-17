using System.Collections;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.Core.Utils;

public static class ObjectUtils
{
    public static readonly JsonSerializerOptions DefaultJsonSeriallizerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static TTarget JsonConvert<TTarget>(this object source, JsonSerializerOptions? jsonSerializerOptions = null)
        => (TTarget)JsonConvert(source, typeof(TTarget), jsonSerializerOptions);

    public static object JsonConvert(this object source, Type targetType, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (targetType.IsEnum && source is string enumStr)
        {
            var enumResult = Enum.Parse(targetType, enumStr, ignoreCase: true);
            return enumResult;
        }

        var sourceStr = source is string str
            ? str
            : source.ToJsonString();

        jsonSerializerOptions ??= DefaultJsonSeriallizerOptions;
        if (string.IsNullOrEmpty(sourceStr))
            return default!;

        var result = JsonSerializer.Deserialize(sourceStr, targetType, jsonSerializerOptions);
        return result!;
    }

    public static object JsonConvert(this string source, Type targetType, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (string.IsNullOrEmpty(source))
            return default!;

        jsonSerializerOptions ??= DefaultJsonSeriallizerOptions;
        var result = JsonSerializer.Deserialize(source, targetType, jsonSerializerOptions);
        return result!;
    }

    public static string ToJsonString(this object source, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return JsonSerializer.Serialize(source, jsonSerializerOptions ?? DefaultJsonSeriallizerOptions);
    }

    public static object? ToType(this string? value, Type type)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException();

        return JsonSerializer.Deserialize(value, type, DefaultJsonSeriallizerOptions);
    }

    public static object? GetPropertyValue(this object target, string propName)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var property = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
            throw new InvalidOperationException($"Property {propName} not found in {target.GetType().Name}");

        return property.GetValue(target);
    }

    public static T SetPropertyValue<T>(this T target, string propName, object? value)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var property = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
            throw new InvalidOperationException($"Property {propName} not found in {target.GetType().Name}");

        property.SetValue(target, value, null);

        return target;
    }

    public static T SetPropertyValue<T, TValue>(this T target, Expression<Func<T, TValue>> memberLambda, TValue value)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (memberLambda.Body is MemberExpression memberSelectorExpression)
        {
            var property = memberSelectorExpression.Member as PropertyInfo;
            var targetObject = GetTargetObject(memberSelectorExpression, target);
            if (property != null && targetObject != null)
            {
                property.SetValue(targetObject, value, null);
            }
        }

        return target;
    }

    private static object GetTargetObject(MemberExpression memberSelectorExpression, object target)
    {
        var expressionStack = new Stack<MemberExpression>();
        while (memberSelectorExpression.Expression is MemberExpression)
        {
            expressionStack.Push(memberSelectorExpression);
            memberSelectorExpression = (MemberExpression)memberSelectorExpression.Expression;
        }

        expressionStack.Push(memberSelectorExpression);

        while (expressionStack.Count > 1)
        {
            var expression = expressionStack.Pop();
            var propertyInfo = expression.Member as PropertyInfo;
            target = propertyInfo?.GetValue(target, null)!;
        }

        return target;
    }

    public static Action<TModel, TKey> BuildSetter<TModel, TKey>(PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        if (!propertyInfo.CanWrite)
            throw new InvalidOperationException($"The property '{propertyInfo.Name}' does not have a setter.");

        var targetType = typeof(TModel);

        // Create parameter expressions
        var targetParameter = Expression.Parameter(targetType, "target");
        var valueParameter = Expression.Parameter(typeof(TKey), "value");

        // Convert the value to the property's type
        var convertedValue = Expression.Convert(valueParameter, propertyInfo.PropertyType);

        // Create a property access expression
        var propertyAccess = Expression.Property(targetParameter, propertyInfo);

        // Create an assignment expression
        var assign = Expression.Assign(propertyAccess, convertedValue);

        // Compile the setter
        var setterLambda = Expression.Lambda<Action<TModel, TKey>>(assign, targetParameter, valueParameter);
        return setterLambda.Compile();
    }

    public static IDictionary<string, object?> FilterProperties(this object obj, IEnumerable<string> propertiesToInclude)
    {
        var filtered = new ExpandoObject() as IDictionary<string, object?>;
        var objType = obj.GetType();

        foreach (var property in propertiesToInclude)
        {
            var propInfo = objType.GetProperty(property);
            if (propInfo != null)
            {
                filtered[property] = propInfo.GetValue(obj);
            }
        }

        return filtered;
    }


    public static IEnumerable<IDictionary<string, object?>> Select(this IEnumerable list, IEnumerable<string> propertiesToInclude)
    {
        foreach (var obj in list)
        {
            yield return FilterProperties(obj, propertiesToInclude);
        }
    }

    public static IEnumerable<object> OrderByProperty(
        this IEnumerable source,
        string propertyName,
        bool isAsc = true)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException(nameof(propertyName));

        var sourceList = source.Cast<object>().ToList();

        // Get the property info from the first item's type
        var property = sourceList.First()
                                  .GetType()
                                  .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property == null)
            throw new ArgumentException($"Property '{propertyName}' does not exist on the type '{sourceList.First().GetType().Name}'.");

        // Sort using reflection to get property values
        var sorted = isAsc
            ? sourceList.OrderBy(x => property.GetValue(x, null))
            : sourceList.OrderByDescending(x => property.GetValue(x, null));

        return sorted;
    }
}
