
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

        public string OriginalName { get; set; } = string.Empty;

        public BaseChildDbModel ChildModel { get; set; } = default!;

        public ICollection<BasicCollectionDbModel> Collection { get; set; } = default!;
    }

    public class BaseChildDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string ChildName { get; set; } = string.Empty;

        public string? ChildDescription { get; set; }
    }

    public class BasicCollectionDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class BaseChildViewModel
    {
        public Guid Id { get; set; }

        public string ChildName { get; set; } = string.Empty;

        public string? ChildDescription { get; set; }
    }

    public class BasicCollectionViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Entity(nameof(BasicViewModel), DbType = typeof(BasicDbModel))]
    public class BasicViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        [EntityPropertyName(nameof(BasicDbModel.OriginalName))]
        public string MapppedName { get; set; } = string.Empty;

        public BaseChildViewModel? ChildModel { get; set; }

        public ICollection<BasicCollectionViewModel>? Collection { get; set; }
    }

    [Fact]
    public async Task BaseViewModelMapping_QueryBaseModel_Success()
    {
        var client = _factory.CreateClient();
        var data = DataGenerator.CreateList<BasicDbModel>(6);
        var db = GetDbContext();
        await db.Set<BasicDbModel>().AddRangeAsync(data);
        await db.SaveChangesAsync();

        data.ForEach(x =>
        {
            x.ChildModel = null!;
        });

        var responseData = await client
            .GetFromJsonAsync<ODataQueryResult<BasicViewModel>>(
            $"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicViewModel)}?$select={nameof(BasicViewModel.Id)}, {nameof(BasicViewModel.Name)}");
        responseData.Should().NotBeNull();
        responseData!.Value.Should().HaveCount(6);
        responseData.Value.Should().BeEquivalentTo(data.Select(x => new BasicViewModel
        {
            Id = x.Id,
            Name = x.Name,
        }));
    }

    [Fact]
    public async Task BaseViewModelMapping_QueryExpandComplexType_Success()
    {
        var client = _factory.CreateClient();
        var data = DataGenerator.CreateList<BasicDbModel>(6);
        var db = GetDbContext();
        await db.Set<BasicDbModel>().AddRangeAsync(data);
        await db.SaveChangesAsync();

        var responseData = await client
            .GetFromJsonAsync<ODataQueryResult<BasicViewModel>>(
            $"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicViewModel)}" +
            $"?$expand={nameof(BasicViewModel.ChildModel)}");
        responseData.Should().NotBeNull();
        responseData!.Value.Should().HaveCount(6);
        responseData.Value.Should().BeEquivalentTo(data.Select(x => new BasicViewModel
        {
            Id = x.Id,
            Name = x.Name,
            MapppedName = x.OriginalName,
            ChildModel = new BaseChildViewModel
            {
                Id = x.ChildModel.Id,
                ChildName = x.ChildModel.ChildName,
                ChildDescription = x.ChildModel.ChildDescription,
            }
        }));
    }

    [Fact]
    public async Task BaseViewModelMapping_QuerySelectExpandComplexType_Success()
    {
        var client = _factory.CreateClient();
        var data = DataGenerator.CreateList<BasicDbModel>(6);
        var db = GetDbContext();
        await db.Set<BasicDbModel>().AddRangeAsync(data);
        await db.SaveChangesAsync();

        var responseData = await client.GetFromJsonAsync<ODataQueryResult<BasicViewModel>>(
            $"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicViewModel)}" +
            $"?$expand={nameof(BasicViewModel.ChildModel)}($select={nameof(BaseChildViewModel.ChildName)})&$select={nameof(BasicViewModel.ChildModel)}");
        responseData.Should().NotBeNull();
        responseData!.Value.Should().HaveCount(6);
        responseData.Value.Should().BeEquivalentTo(data.Select(x => new BasicViewModel
        {
            ChildModel = new BaseChildViewModel
            {
                ChildName = x.ChildModel.ChildName,
            }
        }));
    }

    [Fact]
    public async Task BaseViewModelMapping_QueryExpandCollection_Success()
    {
        // Arrange
        var client = _factory.CreateClient();
        var data = DataGenerator.CreateList<BasicDbModel>(6);
        foreach (var item in data)
        {
            item.Collection = DataGenerator.CreateList<BasicCollectionDbModel>(3);
        }

        var db = GetDbContext();
        await db.Set<BasicDbModel>().AddRangeAsync(data);
        await db.SaveChangesAsync();

        var responseData = await client.GetFromJsonAsync<ODataQueryResult<BasicViewModel>>(
            $"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicViewModel)}" +
            $"?$expand={nameof(BasicViewModel.Collection)}");
        responseData.Should().NotBeNull();
        responseData!.Value.Should().HaveCount(6);
        responseData.Value.Should().BeEquivalentTo(data.Select(x => new BasicViewModel
        {
            Id = x.Id,
            Name = x.Name,
            MapppedName = x.OriginalName,
            Collection = x.Collection.Select(c => new BasicCollectionViewModel
            {
                Id = c.Id,
                Name = c.Name,
            }).ToList()
        }));
    }

    [Fact]
    public async Task BaseViewModelMapping_QuerySelectExpandCollection_Success()
    {
        // Arrange
        var client = _factory.CreateClient();
        var data = DataGenerator.CreateList<BasicDbModel>(6);
        foreach (var item in data)
        {
            item.Collection = DataGenerator.CreateList<BasicCollectionDbModel>(3);
        }
        var db = GetDbContext();
        await db.Set<BasicDbModel>().AddRangeAsync(data);
        await db.SaveChangesAsync();

        // Act
        var responseData = await client.GetFromJsonAsync<ODataQueryResult<BasicViewModel>>(
            $"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicViewModel)}" +
            $"?$expand={nameof(BasicViewModel.Collection)}($select={nameof(BasicCollectionViewModel.Name)})&$select={nameof(BasicViewModel.Collection)}");

        // Assert
        responseData.Should().NotBeNull();
        responseData!.Value.Should().HaveCount(6);
        responseData.Value.Should().BeEquivalentTo(data.Select(x => new BasicViewModel
        {
            Collection = x.Collection.Select(c => new BasicCollectionViewModel
            {
                Name = c.Name,
            }).ToList()
        }));
    }
}
