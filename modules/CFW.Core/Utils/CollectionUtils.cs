using System.Collections;
using System.Linq.Expressions;

namespace CFW.Core.Utils;

public static class CollectionUtils
{
    public static readonly Type[] GenericCollectionTypes = [
        typeof(IEnumerable<>),
        typeof(ICollection<>),
        typeof(IList<>),
        typeof(List<>),
        typeof(ISet<>),
        typeof(HashSet<>)
    ];

    public static bool IsCommonGenericCollectionType(this Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            if (GenericCollectionTypes.Contains(genericTypeDefinition))
            {
                return true;
            }
        }

        return false;
    }

    public static List<T> Random<T>(this IEnumerable<T> source, int count)
    {
        var list = new List<T>(source);
        var random = new Random();
        var result = new List<T>();
        while (result.Count < count || list.Count == 0)
        {
            var index = random.Next(list.Count);
            result.Add(list[index]);
            list.RemoveAt(index);
        }

        return result;
    }

    public static T Random<T>(this IEnumerable<T> source)
    {
        var list = new List<T>(source);
        var random = new Random();
        var index = random.Next(list.Count);
        return list[index];
    }

    public static object Random(this IEnumerable source)
    {
        var list = new List<object>();
        foreach (var item in source)
        {
            list.Add(item);
        }
        var random = new Random();
        var index = random.Next(list.Count);
        return list[index];
    }

    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }
}
