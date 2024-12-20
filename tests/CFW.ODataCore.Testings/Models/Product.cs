using CFW.Core.Entities;
using CFW.ODataCore.OData;

namespace CFW.ODataCore.Testings.Models;

[ODataRouting("products")]
public class Product : IEntity<Guid>, IODataViewModel<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Category? Category { get; set; } = default!;
}