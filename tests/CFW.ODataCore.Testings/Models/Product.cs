using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.Models;

[ConfigurableEntity("products")]
public class Product : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Category? Category { get; set; } = default!;

    public ProductType ProductType { get; set; }
}

public enum ProductType
{
    Unknown,
    Physical,
    Digital,
    Service,
}
