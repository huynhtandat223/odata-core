namespace CFW.ODataCore.OData;

public interface IODataViewModel<TKey>
{
    TKey Id { get; set; }
}
