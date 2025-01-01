using CFW.Core.Entities;
using CFW.ODataCore.Models;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class GetByKeyDbSetAsModelTests : BaseTests, IClassFixture<NonInitAppFactory>
{
    [Entity(nameof(SimpleGetByKeyEntity), Methods = [ODataHttpMethod.GetByKey])]
    public class SimpleGetByKeyEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public GetByKeyDbSetAsModelTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory)
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
                    .AddODataMinimalApi(new TestMetadataContainerFactory(typeof(SimpleGetByKeyEntity)));
            });
        });
    }

    [Fact]
    public async Task GetByKey_Success()
    {
        // Arrange
        var expected = DataGenerator.Create<SimpleGetByKeyEntity>();
        var dbContext = GetDbContext();
        dbContext.Set<SimpleGetByKeyEntity>().Add(expected);
        await dbContext.SaveChangesAsync();

        var httpClient = _factory.CreateClient();

        // Act
        var response = await httpClient
            .GetFromJsonAsync<SimpleGetByKeyEntity>($"{Constants.DefaultODataRoutePrefix}/{nameof(SimpleGetByKeyEntity)}/{expected.Id}");

        // Assert
        response.Should().NotBeNull();
        response!.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetByKey_SelectProps_Success()
    {
        // Arrange
        var expected = DataGenerator.Create<SimpleGetByKeyEntity>();
        var dbContext = GetDbContext();
        dbContext.Set<SimpleGetByKeyEntity>().Add(expected);
        await dbContext.SaveChangesAsync();

        var httpClient = _factory.CreateClient();

        // Act
        var response = await httpClient
            .GetFromJsonAsync<SimpleGetByKeyEntity>($"{Constants.DefaultODataRoutePrefix}/{nameof(SimpleGetByKeyEntity)}/{expected.Id}" +
            $"?$select={nameof(SimpleGetByKeyEntity.Id)}");

        // Assert
        response.Should().NotBeNull();
        response!.Should().NotBeNull();

        response!.Id.Should().Be(expected.Id);

        response!.Name.Should().BeNullOrEmpty();
    }
}
