using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class PatchByKeyDbSetAsModelTests : BaseTests, IAssemblyFixture<NonInitAppFactory>
{
    [Entity(nameof(PatchByKeyEntity), Methods = [ODataHttpMethod.Patch])]
    public class PatchByKeyEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public PatchByKeyDbSetAsModelTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory)
        : base(testOutputHelper, factory, typeof(PatchByKeyEntity))
    {

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
