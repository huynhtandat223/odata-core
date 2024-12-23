using CFW.Core.Entities;
using CFW.ODataCore.Features.EntityCreate;

namespace CFW.ODataCore.Testings.Models;

[ODataEntitySet("categories")]
[EntityCreate<Category, Guid>("categories")]
public class Category : IEntity<Guid>, IODataViewModel<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Slug { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}

[ODataEntitySet("orders")]
public class Order : IEntity<Guid>, IODataViewModel<Guid>
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