
using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.TestCases.ViewModelTests;

public class BasicViewModelTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public BasicViewModelTests(ITestOutputHelper testOutputHelper
        , AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    public class BasicDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public BaseChildModel ChildModel { get; set; } = default!;
    }

    public class BaseChildModel : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string ChildName { get; set; } = string.Empty;

        public string? ChildDescription { get; set; }
    }

    public class BaseChildViewModel
    {
        public Guid Id { get; set; }

        public string ChildName { get; set; } = string.Empty;

        public string? ChildDescription { get; set; }
    }

    [Entity(nameof(BasicViewModel), DbType = typeof(BasicDbModel))]
    public class BasicViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public BaseChildViewModel? ChildModel { get; set; }
    }

    [Fact]
    public async Task BaseViewModelMapping_Query_Success()
    {
        var client = _factory.CreateClient();
        var data = DataGenerator.CreateList<BasicDbModel>(6);
        var db = GetDbContext();
        await db.Set<BasicDbModel>().AddRangeAsync(data);
        await db.SaveChangesAsync();

        var responseData = await client
            .GetFromJsonAsync<ODataQueryResult<BasicViewModel>>(
            $"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicViewModel)}?$expand={nameof(BasicViewModel.ChildModel)}");
        responseData.Should().NotBeNull();
        responseData!.Value.Should().HaveCount(6);
        responseData.Value.Should().BeEquivalentTo(data.Select(x => new BasicViewModel
        {
            Id = x.Id,
            Name = x.Name
        }));
    }
}
