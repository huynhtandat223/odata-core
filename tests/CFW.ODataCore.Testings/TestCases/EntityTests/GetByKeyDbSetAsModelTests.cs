using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class GetByKeyDbSetAsModelTests : BaseTests, IAssemblyFixture<AppFactory>
{
    [Entity(nameof(SimpleGetByKeyEntity), Methods = [ApiMethod.GetByKey])]
    public class SimpleGetByKeyEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public GetByKeyDbSetAsModelTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(SimpleGetByKeyEntity)])
    {

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
