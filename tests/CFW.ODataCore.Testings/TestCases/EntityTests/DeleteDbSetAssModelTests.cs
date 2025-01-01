
using CFW.Core.Entities;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class DeleteDbSetAssModelTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public DeleteDbSetAssModelTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Entity(nameof(SimpleDeleteEntity))]
    public class SimpleDeleteEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Delete_Success()
    {
        // Arrange
        var expected = DataGenerator.Create<SimpleDeleteEntity>();
        var dbContext = GetDbContext();
        dbContext.Set<SimpleDeleteEntity>().Add(expected);
        await dbContext.SaveChangesAsync();
        var httpClient = _factory.CreateClient();
        // Act
        var response = await httpClient.DeleteAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(SimpleDeleteEntity)}/{expected.Id}");
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
