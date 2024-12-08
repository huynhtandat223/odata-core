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
}
