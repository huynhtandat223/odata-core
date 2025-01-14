
using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.TestCases.ViewModelTests;

public class ViewModelPatchTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public ViewModelPatchTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Entity(nameof(PatchDbModel))]
    public class PatchDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string OriginalName { get; set; } = string.Empty;

        public PatchChildDbModel? Child { get; set; } = default!;

        public ICollection<PatchDbModel>? Children { get; set; } = default!;
    }

    public class PatchChildDbModel : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string ChildName { get; set; } = string.Empty;

        public string ChildDescription { get; set; } = string.Empty;
    }

    public class PatchChildCollection : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    public class PatchChildViewModel
    {
        public Guid Id { get; set; }

        public string ChildName { get; set; } = string.Empty;

        public string ChildDescription { get; set; } = string.Empty;
    }

    public class PatchChildCollectionViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    [Entity(nameof(PatchViewModel)
        //, DbType = typeof(PatchDbModel)
        )]
    public class PatchViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [EntityPropertyName(nameof(PatchDbModel.OriginalName))]
        public string MappingName { get; set; } = string.Empty;

        public PatchChildViewModel? Child { get; set; }

        public List<PatchChildViewModel>? Children { get; set; }
    }

    [Fact]
    public async Task PatchViewModel_PrimaryProps_Success()
    {
        // Arrange
        var dbModel = DataGenerator.Create<PatchDbModel>();
        dbModel.Child = null;

        var db = GetDbContext();
        db.Add(dbModel);
        db.SaveChanges();

        var patchEntity = new Dictionary<string, object?>
        {
            { nameof(PatchViewModel.Name), DataGenerator.NewGuidString() },
            { nameof(PatchViewModel.MappingName), DataGenerator.NewGuidString() }
        };
        var client = _factory.CreateClient();
        var baseUrl = typeof(PatchViewModel).GetBaseUrl();

        // Act
        var patchResp = await client.PatchAsync($"{baseUrl}/{dbModel.Id}"
            , patchEntity.ToStringContent());

        // Assert
        patchResp.IsSuccessStatusCode.Should().BeTrue();
        var dbEntity = GetDbContext().Set<PatchDbModel>().Find(dbModel.Id);
        dbEntity.Should().NotBeNull();

        dbEntity!.Name.Should().Be(patchEntity[nameof(PatchViewModel.Name)]!.ToString());
        dbEntity.OriginalName.Should().Be(patchEntity[nameof(PatchViewModel.MappingName)]!.ToString());

        dbEntity.Description.Should().Be(dbModel.Description);
    }

    [Fact]
    public async Task PatchViewModel_ComplexProp_Error()
    {
        // Arrange
        var dbModel = DataGenerator.Create<PatchDbModel>();
        dbModel.Children = null;
        var db = GetDbContext();
        db.Add(dbModel);
        db.SaveChanges();

        var patchEntity = new Dictionary<string, object?>
        {
            { nameof(PatchViewModel.Child), new PatchChildViewModel
                {
                    ChildName = DataGenerator.NewGuidString(),
                    ChildDescription = DataGenerator.NewGuidString()
                }
            }
        };
        var client = _factory.CreateClient();
        var baseUrl = typeof(PatchViewModel).GetBaseUrl();
        // Act
        var patchResp = await client.PatchAsync($"{baseUrl}/{dbModel.Id}"
            , patchEntity.ToStringContent());

        // Assert
        patchResp.IsSuccessStatusCode.Should().BeFalse();
    }
}
