namespace CFW.ODataCore.Features.BoundActions;

[AttributeUsage(AttributeTargets.Class)]
public class BoundActionAttribute : Attribute
{
    public Type ViewModelType { get; set; } = default!;

    public Type KeyType { get; set; } = default!;

    public string Name { get; set; } = default!;
}

public class BoundActionAttribute<TODataViewModel, TKey> : BoundActionAttribute
{
    public BoundActionAttribute(string name)
    {
        Name = name;
        ViewModelType = typeof(TODataViewModel);
        KeyType = typeof(TKey);
    }
}