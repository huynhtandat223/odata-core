
using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class QueryDbSetAsModelTests : BaseTests, IAssemblyFixture<AppFactory>
{
    [Entity(nameof(SimpleQueryEntity), Methods = [EntityMethod.Query])]
    public class SimpleQueryEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    public QueryDbSetAsModelTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(SimpleQueryEntity)])
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


    [Fact]
    public async Task Query_WithSelect_Success()
    {
        // Arrange
        var entities = DataGenerator.CreateList<SimpleQueryEntity>(6);
        var dbContext = GetDbContext();
        await dbContext.Set<SimpleQueryEntity>().AddRangeAsync(entities);
        await dbContext.SaveChangesAsync();
        var httpClient = _factory.CreateClient();

        // Act
        var responseEntities = await httpClient
            .GetFromJsonAsync<ODataQueryResult<SimpleQueryEntity>>($"{Constants.DefaultODataRoutePrefix}" +
            $"/{nameof(SimpleQueryEntity)}?$select={nameof(SimpleQueryEntity.Name)},{nameof(SimpleQueryEntity.Description)}");

        // Assert
        responseEntities.Should().NotBeNull();
        responseEntities!.Value.Should().NotBeNull();
        responseEntities!.Value.Should().HaveCount(entities.Count);

        var expected = entities.Select(x => new SimpleQueryEntity
        {
            Name = x.Name,
            Description = x.Description
        }).ToList();

        var actual = responseEntities.Value.Select(x => new SimpleQueryEntity
        {
            Name = x.Name,
            Description = x.Description
        }).ToList();

        expected!.Should().BeEquivalentTo(actual);
    }
}
