
using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class QueryDbSetAsModelTests : BaseTests, IAssemblyFixture<NonInitAppFactory>
{
    [Entity(nameof(SimpleQueryEntity), Methods = [EntityMethod.Query])]
    public class SimpleQueryEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public QueryDbSetAsModelTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory)
        : base(testOutputHelper, factory, typeof(SimpleQueryEntity))
    {

    }

    [Fact]
    public async Task Query_Success()
    {
        // Arrange
        var entities = DataGenerator.CreateList<SimpleQueryEntity>(6);
        var dbContext = GetDbContext();
        await dbContext.Set<SimpleQueryEntity>().AddRangeAsync(entities);
        await dbContext.SaveChangesAsync();

        var httpClient = _factory.CreateClient();

        // Act
        var responseEntities = await httpClient
            .GetFromJsonAsync<ODataQueryResult<SimpleQueryEntity>>($"{Constants.DefaultODataRoutePrefix}/{nameof(SimpleQueryEntity)}");

        // Assert
        responseEntities.Should().NotBeNull();
        responseEntities!.Value.Should().NotBeNull();
        responseEntities!.Value.Should().HaveCount(entities.Count);
        responseEntities.TotalCount = null;

        responseEntities!.Value.Should().BeEquivalentTo(entities);
    }

    [Fact]
    public async Task Query_WithCount_Success()
    {
        // Arrange
        var entities = DataGenerator.CreateList<SimpleQueryEntity>(6);
        var dbContext = GetDbContext();
        await dbContext.Set<SimpleQueryEntity>().AddRangeAsync(entities);
        await dbContext.SaveChangesAsync();

        var httpClient = _factory.CreateClient();

        var responseEntities = await httpClient
            .GetFromJsonAsync<ODataQueryResult<SimpleQueryEntity>>($"{Constants.DefaultODataRoutePrefix}/{nameof(SimpleQueryEntity)}?$count=true");

        // Assert
        responseEntities.Should().NotBeNull();
        responseEntities!.Value.Should().NotBeNull();
        responseEntities!.Value.Should().HaveCount(entities.Count);
        responseEntities.TotalCount = entities.Count;

        responseEntities!.Value.Should().BeEquivalentTo(entities);
    }
}
