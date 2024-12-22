using CFW.ODataCore.Core;

namespace CFW.ODataCore.Features.BoundOperations;

[AttributeUsage(AttributeTargets.Class)]
public class BoundOperationAttribute : Attribute
{
    public Type ViewModelType { get; set; } = default!;

    public Type KeyType { get; set; } = default!;

    public string Name { get; set; } = default!;

    public OperationType OperationType { get; set; } = default!;
}

public class BoundActionAttribute<TODataViewModel, TKey> : BoundOperationAttribute
{
    public BoundActionAttribute(string name)
    {
        Name = name;
        ViewModelType = typeof(TODataViewModel);
        KeyType = typeof(TKey);
        OperationType = OperationType.Action;
    }
}

public class BoundFunctionAttribute<TODataViewModel, TKey> : BoundOperationAttribute
{
    public BoundFunctionAttribute(string name)
    {
        Name = name;
        ViewModelType = typeof(TODataViewModel);
        KeyType = typeof(TKey);
        OperationType = OperationType.Function;
    }
}