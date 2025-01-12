using CFW.ODataCore.Projectors.EFCore;
using CFW.ODataCore.Testings.Models;
using CFW.ODataCore.Testings.TestCases;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Testings.UseCases;

public class EntityCreateDefaultConfigureTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EntityCreateDefaultConfigureTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Product), typeof(Category), typeof(Order)])
    {
    }

    [Theory]
    [InlineData(typeof(Category), null)]
    [InlineData(typeof(Product), null)]
    [InlineData(typeof(Order), nameof(Product.Category))]
    public async Task CreateEntity_DefaultConfigure_Success(Type dbModelType, string nestedCollectionProperty)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetBaseUrl();
        var entity = DataGenerator.Create(dbModelType);
        var complexProps = dbModelType.GetComplexTypeProperties();
        var collectionProps = dbModelType.GetCollectionTypeProperties();

        if (collectionProps.Any())
        {
            foreach (var collectionProp in collectionProps)
            {
                var collectionPropElementType = dbModelType.GetProperty(collectionProp)
                    .PropertyType.GetGenericArguments()[0];
                var collection = DataGenerator.CreateList(collectionPropElementType, 3);

                foreach (var item in collection)
                {
                    item.SetPropertyValue(nestedCollectionProperty, null);
                }

                entity.SetPropertyValue(collectionProp, collection);
            }
        }

        // Act
        var response = await client.PostAsJsonAsync(baseUrl, entity);

        // Assert
        response.Should().BeSuccessful();

        var db = GetDbContext();

        var id = entity.GetPropertyValue(DefaultIdProp);
        var actual = await db.LoadAsync(dbModelType, [id!], complexProps, collectionProps);

        actual.Should().BeEquivalentTo(entity, o => o.WithoutStrictOrdering());
    }
}
