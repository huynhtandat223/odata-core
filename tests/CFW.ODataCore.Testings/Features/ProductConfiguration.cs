using CFW.ODataCore.Testings.Models;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Testings.Features;

//[ConfigurableEntity("products")]
public class ProductConfiguration : EntityConfiguration<Product>
{
    public ProductConfiguration()
    {
        EnableQuery<TestingDbContext>(db => db.Set<Product>().AsNoTracking());
    }

    public override string ToString()
    {
        return nameof(ProductConfiguration);
    }
}
