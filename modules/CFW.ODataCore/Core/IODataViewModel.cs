namespace CFW.ODataCore.Core;

public interface IODataViewModel<TKey>
{
    TKey Id { get; set; }
}
