using CFW.Core.Entities;
using CFW.ODataCore.Core;

namespace CFW.ODataCore.Tests.Models;

[ODataRouting("categories")]
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