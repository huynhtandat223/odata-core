using CFW.Core.Entities;
using CFW.ODataCore.Models;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class CreateDbSetAsModelTests : BaseTests, IAssemblyFixture<AppFactory>
{
    [Entity(nameof(SimpleEntity))]
    public class SimpleEntity : IEntity<Guid>
    {
        public Guid Id { set; get; }

        public string? Name { set; get; }
    }

    [Entity(nameof(SetupPostEntity), Methods = [ApiMethod.Post])]
    public class SetupPostEntity : IEntity<Guid>
    {
        public Guid Id { set; get; }

        public string? Name { set; get; }
    }

    [Entity("withoutPostSimpleEntities", Methods = [ApiMethod.Query])]
    public class WithoutPostSimpleEntity : IEntity<Guid>
    {
        public Guid Id { set; get; }

        public string? Name { set; get; }
    }

    public CreateDbSetAsModelTests(ITestOutputHelper testOutputHelper, AppFactory factory)
            : base(testOutputHelper, factory)
    {
    }

    [Fact]
    public async Task NoPostMethod_Should_ReturnNotAllow()
    {
        // Arrange
        var expected = DataGenerator.Create<WithoutPostSimpleEntity>();
        var httpClient = _factory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync($"odata-api/withoutPostSimpleEntities", expected);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Create_AttributeSetupPost_Success()
    {
        // Arrange
        var expected = DataGenerator.Create<SetupPostEntity>();
        var httpClient = _factory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(SetupPostEntity)}", expected);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dbContext = GetDbContext();
        var actualEntity = await dbContext.Set<SetupPostEntity>().FindAsync(expected.Id);

        actualEntity.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Create_RuntimeDbSet_Success()
    {
        // Arrange
        var expected = DataGenerator.Create<SimpleEntity>();
        var httpClient = _factory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(SimpleEntity)}", expected);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dbContext = GetDbContext();
        var actualEntity = await dbContext.Set<SimpleEntity>().FindAsync(expected.Id);

        actualEntity.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Create_ConfiguredDbSet_Success()
    {
        // Arrange
        var expected = DataGenerator.Create<ConfiguredDbSet>();
        var httpClient = _factory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync($"odata-api/{ConfiguredDbSet.RoutingName}", expected);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dbContext = GetDbContext();
        var actualEntity = await dbContext.ConfiguredDbSets.FindAsync(expected.Id);

        actualEntity.Should().BeEquivalentTo(expected);
    }
}
