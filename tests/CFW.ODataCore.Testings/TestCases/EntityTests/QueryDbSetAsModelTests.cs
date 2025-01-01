
using CFW.Core.Entities;
using CFW.ODataCore.Models;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class QueryDbSetAsModelTests : BaseTests, IClassFixture<NonInitAppFactory>
{
    [Entity(nameof(SimpleQueryEntity), Methods = [ODataHttpMethod.Query])]
    public class SimpleQueryEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public QueryDbSetAsModelTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory)
        : base(testOutputHelper, factory)
    {
        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var dbDir = Path.Combine(currentDirectory, "testDbs");
                if (!Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }
                var dbPath = Path.Combine(dbDir, $"appdbcontext_{Guid.NewGuid()}.db");
                services.AddDbContext<TestingDbContext>(
                           options => options
                           .ReplaceService<IModelCustomizer, ODataModelCustomizer<TestingDbContext>>()
                           .EnableSensitiveDataLogging()
                           .UseSqlite($"Data Source={dbPath}"));

                services.AddControllers()
                .AddODataMinimalApi(new TestMetadataContainerFactory(typeof(SimpleQueryEntity)));
            });
        });
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
