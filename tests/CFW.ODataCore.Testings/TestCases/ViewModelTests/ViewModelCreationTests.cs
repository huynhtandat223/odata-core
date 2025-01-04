
using CFW.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Testings.TestCases.ViewModelTests;

public class ViewModelCreationTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public ViewModelCreationTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {
    }

    public class BasicCreationDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string OriginalName { get; set; } = string.Empty;

        public BasicCreationChildDbModel? Child { get; set; }

        public List<BasicCreationChildDbModel>? Children { get; set; }
    }

    public class BasicCreationChildDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string ChildName { get; set; } = string.Empty;

        public string? ChildDescription { get; set; }
    }

    public class BasicCreationCollectionDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Entity(nameof(BasicCreationViewModel), DbType = typeof(BasicCreationDbModel))]
    public class BasicCreationViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string OriginalName { get; set; } = string.Empty;

        public BasicCreationChildViewModel? Child { get; set; }

        public List<BasicCreationChildViewModel>? Children { get; set; }
    }

    public class BasicCreationChildViewModel
    {
        public Guid Id { get; set; }

        public string ChildName { get; set; } = string.Empty;

        public string? ChildDescription { get; set; }
    }

    public class BasicCreationCollectionViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Create_ViewModel_PrimitaryProps_Success()
    {
        // Arrange
        var viewModel = DataGenerator.Create<BasicCreationViewModel>();
        viewModel.Child = null;

        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicCreationViewModel)}", viewModel);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var db = GetDbContext();
        var dbModel = await db.Set<BasicCreationDbModel>().FirstOrDefaultAsync(x => x.Id == viewModel.Id);
        dbModel.Should().BeEquivalentTo(viewModel);
    }

    [Fact]
    public async Task Create_ViewModel_WithComplexChild_Success()
    {
        // Arrange
        var viewModel = DataGenerator.Create<BasicCreationViewModel>();

        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicCreationViewModel)}", viewModel);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var db = GetDbContext();
        var dbModel = await db.Set<BasicCreationDbModel>()
            .Include(x => x.Child)
            .FirstOrDefaultAsync(x => x.Id == viewModel.Id);
        dbModel.Should().BeEquivalentTo(viewModel);
    }

    [Fact]
    public async Task Create_ViewModel_WithCollection_Success()
    {
        // Arrange
        var viewModel = DataGenerator.Create<BasicCreationViewModel>();
        viewModel.Child = null;
        viewModel.Children = DataGenerator.CreateList<BasicCreationChildViewModel>(5);
        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicCreationViewModel)}", viewModel);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var db = GetDbContext();
        var dbModel = await db.Set<BasicCreationDbModel>()
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == viewModel.Id);
        dbModel.Should().BeEquivalentTo(viewModel);
    }

    [Fact]
    public async Task Create_ViewModel_WithComplexPropertyAndCollection_Success()
    {
        // Arrange
        var viewModel = DataGenerator.Create<BasicCreationViewModel>();
        viewModel.Children = DataGenerator.CreateList<BasicCreationChildViewModel>(5);
        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(BasicCreationViewModel)}", viewModel);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var db = GetDbContext();
        var dbModel = await db.Set<BasicCreationDbModel>()
            .Include(x => x.Child)
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == viewModel.Id);
        dbModel.Should().BeEquivalentTo(viewModel);
    }
}
