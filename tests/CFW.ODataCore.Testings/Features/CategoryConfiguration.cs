using CFW.ODataCore.Testings.Models;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Testings.Features;

//[ConfigurableEntity("categories")]
public class CategoryConfiguration : EntityConfiguration<Category>
{
    /// <summary>
    /// "categories" is the endpoint name.
    /// </summary>
    public CategoryConfiguration()
    {
        EnableQuery<TestingDbContext>(db => db.Set<Category>().AsNoTracking());
    }

    //public override Expression<Func<Category, object>> EntityViewModel => (c) => new
    //{
    //    c.Id,
    //    c.Name,
    //    c.Description,
    //    c.Slug,
    //    ParentId = c.ParentCategoryId, // ParentId is an alias for ParentCategoryId in the response.
    //    c.ImageUrl,
    //    c.IsActive,
    //    c.DisplayOrder,
    //    c.CreatedAt,
    //    c.UpdatedAt,
    //    //c.DeletedAt   -> This property is not present in the response.
    //};

    //Testing format
    public override string ToString()
    {
        return nameof(CategoryConfiguration);
    }
}