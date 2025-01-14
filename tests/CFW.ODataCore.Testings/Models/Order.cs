using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.Models;

[EntityV2("orders")]
public class Order : IEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string? OrderNumber { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}