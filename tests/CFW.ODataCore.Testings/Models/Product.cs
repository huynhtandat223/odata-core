using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.Models;

[Entity("products")]
public class Product : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Category? Category { get; set; } = default!;
}