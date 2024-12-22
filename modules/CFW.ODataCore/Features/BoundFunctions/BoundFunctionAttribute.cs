namespace CFW.ODataCore.Features.BoundActions;


public class BoundFunctionAttribute<TODataViewModel, TKey> : BoundOperationAttribute
{
    public BoundFunctionAttribute(string name)
    {
        Name = name;
        ViewModelType = typeof(TODataViewModel);
        KeyType = typeof(TKey);
    }
}