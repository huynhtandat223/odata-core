using CFW.Core.Entities;

namespace CFW.EntityMimimalApi.Samples.Models;

[Entity("categories")]
public class Category : IEntity<Guid>
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
