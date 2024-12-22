using CFW.Core.Entities;

namespace CFW.ODataCore.Extensions;

/// <summary>
/// Use for nameof operator, not for actual use
/// </summary>
public class RefODataViewModel : IODataViewModel<int>, IEntity<int>
{
    public int Id { get; set; }
}
