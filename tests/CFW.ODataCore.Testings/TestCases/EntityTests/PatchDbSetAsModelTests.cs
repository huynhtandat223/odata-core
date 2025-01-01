using CFW.Core.Entities;
using CFW.ODataCore.Models;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class PatchByKeyDbSetAsModelTests : BaseTests, IClassFixture<NonInitAppFactory>
{
    [Entity(nameof(PatchByKeyEntity), Methods = [ODataHttpMethod.Patch])]
    public class PatchByKeyEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public PatchByKeyDbSetAsModelTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory)
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
                    .AddODataMinimalApi(new TestMetadataContainerFactory(typeof(PatchByKeyEntity)));
            });
        });
    }

    [Fact]
    public async Task SimplePatch_Success()
    {
        // Arrange
        var expected = DataGenerator.Create<PatchByKeyEntity>();
        var dbContext = GetDbContext();
        dbContext.Set<PatchByKeyEntity>().Add(expected);
        await dbContext.SaveChangesAsync();

        var patchEntity = DataGenerator
            .Create<PatchByKeyEntity>()
            .SetPropertyValue(x => x.Id, expected.Id);

        var httpClient = _factory.CreateClient();

        // Act
        var response = await httpClient
            .PatchAsJsonAsync($"odata-api/{nameof(PatchByKeyEntity)}/{expected.Id}", patchEntity);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var newDb = GetDbContext();
        var actual = await newDb.Set<PatchByKeyEntity>().FindAsync(expected.Id);
        actual.Should().NotBeNull();
        actual!.Should().BeEquivalentTo(patchEntity);
    }
}
