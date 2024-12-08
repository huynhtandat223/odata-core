using CFW.Core.Entities;
using CFW.ODataCore.Core;

namespace CFW.ODataCore.Tests.Models;

[ODataRouting("customers")]
public class Customer : IEntity<int>, IODataViewModel<int>
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }
}
