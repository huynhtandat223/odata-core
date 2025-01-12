using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class NewTest : BaseTests, IAssemblyFixture<AppFactory>
{
    public class EntityEndpointConfgurationDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTimeOffset TrackingDate { get; set; }

        public ChildEntityEndpointConfgurationDbModel? Child { get; set; }

        public ICollection<ChildCollectionEntityEndpointConfgurationDbModel>? Children { get; set; } = default!;
    }

    public class ChildEntityEndpointConfgurationDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid ParentId { get; set; }
    }

    public class ChildCollectionEntityEndpointConfgurationDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid ParentId { get; set; }
    }

    public NewTest(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {
    }

    [Fact]
    public async Task Test()
    {
        //// Arrange
        //var client = _factory.CreateClient();

        //var data = DataGenerator.CreateList<EntityEndpointConfgurationDbModel>(6);

        //foreach (var item in data)
        //{
        //    item.Children = DataGenerator.CreateList<ChildCollectionEntityEndpointConfgurationDbModel>(3);
        //}

        //var db = GetDbContext();
        //db.AddRange(data);
        //await db.SaveChangesAsync();
        //var baseUrl = typeof(EntityEndpointConfgurationDbModelEndpointBuilder).GetBaseUrl();

        //// Act
        //var respR = await client.GetAsync($"{baseUrl}?$select=newName,child&$expand=child,children");
        //var content = await respR.Content.ReadAsStringAsync();

        //// Assert
        //var resp = await respR.Content.ReadFromJsonAsync<ODataQueryResult<EntityEndpointConfgurationDbModel>>();

        //resp.Value.Count().Should().Be(6);
        //var names = resp.Value.Select(x => x.Name).ToList();
        //names.Should().BeEquivalentTo(data.Select(x => x.Name));
    }
}
