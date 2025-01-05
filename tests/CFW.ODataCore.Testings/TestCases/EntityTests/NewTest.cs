using CFW.Core.Entities;
using CFW.ODataCore.Models;
using System.Linq.Expressions;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class NewTest : BaseTests, IAssemblyFixture<NonInitAppFactory>
{
    [Entity(nameof(EntityEndpointConfgurationDbModel), Methods = [EntityMethod.Query])]
    public class EntityEndpointConfgurationDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public interface IEntityEndpointConfiguration
    {

    }

    public class EntityEndpointConfgurationDbModelConfguration : IEntityEndpointConfiguration
    {
        public Expression<Func<EntityEndpointConfgurationDbModel, object>> Query
            => x => new
            {
                x.Id,
                NewName = x.Name
            };
    }

    public NewTest(ITestOutputHelper testOutputHelper, NonInitAppFactory factory)
        : base(testOutputHelper, factory, typeof(EntityEndpointConfgurationDbModel))
    {
    }

    [Fact]
    public void Test2()
    {

    }

    [Fact]
    public async Task Test()
    {
        // Arrange
        var client = _factory.CreateClient();

        var data = DataGenerator.CreateList<EntityEndpointConfgurationDbModel>(6);
        var db = GetDbContext();
        db.AddRange(data);
        await db.SaveChangesAsync();
        var baseUrl = typeof(EntityEndpointConfgurationDbModel).GetBaseUrl();

        // Act
        var respR = await client.GetAsync($"{baseUrl}?$select=newName");
        var content = await respR.Content.ReadAsStringAsync();


        // Assert
        var resp = await respR.Content.ReadFromJsonAsync<ODataQueryResult<EntityEndpointConfgurationDbModel>>();
        resp.Value.Count().Should().Be(6);
        var names = resp.Value.Select(x => x.Name).ToList();
        names.Should().BeEquivalentTo(data.Select(x => x.Name));
    }
}
