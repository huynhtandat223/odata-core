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
}
