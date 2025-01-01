using CFW.Core.Entities;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.EntityTests;

public class CreateDbSetAsModelWithHandlerTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public CreateDbSetAsModelWithHandlerTests(ITestOutputHelper testOutputHelper
        , AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    public class OverridedEntity : IEntity<Guid>
    {
        public Guid Id { set; get; }

        public string? Name { set; get; }
    }

    [Entity(nameof(OverridedEntity))]
    public class OverrideCreateHandler : IEntityCreateHandler<OverridedEntity>
    {
        private readonly List<object> _requests;
        public OverrideCreateHandler(List<object> requests)
        {
            _requests = requests;
        }

        public async Task<Result<OverridedEntity>> Handle(OverridedEntity request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return await Task.FromResult(request.Created());
        }
    }

    [Entity(nameof(OverridedEntity))]
    public class OverrideDeleteHandler : IEntityDeleteHandler<OverridedEntity, Guid>
    {
        private readonly List<object> _requests;
        public OverrideDeleteHandler(List<object> request)
        {
            _requests = request;
        }

        public async Task<Result> Handle(Guid key, CancellationToken cancellationToken)
        {
            _requests.Add(key);
            return await Task.FromResult(this.Success());
        }
    }

    [Fact]
    public async Task Create_HasOverrideHandler_Should_ExecuteOverrideHandler()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var expected = DataGenerator.Create<OverridedEntity>();

        // Act
        var response = await httpClient.PostAsJsonAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(OverridedEntity)}", expected);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var requests = _factory.Services.GetRequiredService<List<object>>();

        var createHandlerRequest = requests.OfType<OverridedEntity>().Single();
        createHandlerRequest.Should().BeEquivalentTo(expected);


        var deleteResponse = await httpClient.DeleteAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(OverridedEntity)}/{expected.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var deleteHandlerRequest = requests.OfType<Guid>().Single();
        deleteHandlerRequest.Should().Be(expected.Id);
    }
}
