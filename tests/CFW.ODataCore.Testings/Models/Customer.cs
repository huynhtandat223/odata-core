using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.Models;

[Entity("customers")]
public class Customer : IEntity<int>
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }
}
