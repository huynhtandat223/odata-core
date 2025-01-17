using CFW.ODataCore.Testings.Models;

namespace CFW.ODataCore.Testings.TestCases;

public class DbEntityCreateDefaultConfigureTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public DbEntityCreateDefaultConfigureTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Product), typeof(Category), typeof(Order)])
    {
    }

    [Theory]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    [InlineData(typeof(Order))]
    public async Task CreateEntity_DefaultConfigure_Success(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var entity = DataGenerator.Create(dbModelType);

        // Act
        var response = await client.PostAsJsonAsync(baseUrl, entity);

        // Assert
        response.Should().BeSuccessful();

        var db = GetDbContext();
        var id = entity.GetPropertyValue(DefaultIdProp);
        var actual = await db.LoadAsync(dbModelType, [id!]);

        actual.Should().BeEquivalentTo(entity, o => o.WithoutStrictOrdering());
    }
}
