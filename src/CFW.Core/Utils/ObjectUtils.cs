using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace CFW.Core.Utils;

public static class ObjectUtils
{
    private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static TTarget JsonConvert<TTarget>(this object source)
    {
        if (typeof(TTarget).IsEnum && source is string enumStr)
        {
            var enumResult = Enum.Parse(typeof(TTarget), enumStr, ignoreCase: true);
            return (TTarget)enumResult;
        }

        string sourceStr = source switch
        {
            string str => str,
            _ => JsonSerializer.Serialize(source, _serializerOptions)
        };

        if (string.IsNullOrEmpty(sourceStr))
            return default!;

        var result = JsonSerializer.Deserialize<TTarget>(sourceStr, _serializerOptions);
        return result!;
    }

    public static string ToJsonString(this object source)
    {
        return JsonSerializer.Serialize(source, _serializerOptions);
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
}
