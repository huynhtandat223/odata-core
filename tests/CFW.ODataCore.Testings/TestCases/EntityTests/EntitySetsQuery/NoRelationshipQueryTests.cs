using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.TestCases.EntityTests.EntitySetsQuery;

public class NoRelationshipQueryTests : BaseTests, IClassFixture<AppFactory>
{
    public NoRelationshipQueryTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(NoRelationshipQueryViewModel)])
    {

    }

    [Entity(nameof(NoRelationshipQueryViewModel), Methods = [ApiMethod.Query])]
    public class NoRelationshipQueryViewModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Query_ValidData_ShouldSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        var entities = DataGenerator.CreateList<NoRelationshipQueryViewModel>(6);
        var db = GetDbContext();
        db.Set<NoRelationshipQueryViewModel>().AddRange(entities);
        await db.SaveChangesAsync();
        var baseUrl = $"{Constants.DefaultODataRoutePrefix}/{nameof(NoRelationshipQueryViewModel)}";

        // Act
        var actualEntities = await client.GetFromJsonAsync<ODataQueryResult<NoRelationshipQueryViewModel>>(baseUrl);

        // Assert
        actualEntities.Should().NotBeNull();
        actualEntities!.Value.Should().NotBeNull();
        actualEntities!.Value!.Count().Should().Be(6);
        actualEntities!.Value.Should().BeEquivalentTo(entities);
    }

    [Fact]
    public async Task Query_ValidData_Top_ShouldSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        var entities = DataGenerator.CreateList<NoRelationshipQueryViewModel>(6);
        var db = GetDbContext();
        db.Set<NoRelationshipQueryViewModel>().AddRange(entities);
        await db.SaveChangesAsync();
        var baseUrl = $"{Constants.DefaultODataRoutePrefix}/{nameof(NoRelationshipQueryViewModel)}?$top=5&$count=true";

        // Act
        var actualEntities = await client.GetFromJsonAsync<ODataQueryResult<NoRelationshipQueryViewModel>>(baseUrl);

        // Assert
        actualEntities.Should().NotBeNull();
        actualEntities!.Value.Should().NotBeNull();
        actualEntities!.Value!.Count().Should().Be(5);

        actualEntities.TotalCount.Should().Be(6);
    }
}
