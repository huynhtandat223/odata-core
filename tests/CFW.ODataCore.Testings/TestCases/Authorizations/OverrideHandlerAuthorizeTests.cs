using CFW.Core.Entities;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Authorizations;

public class OverrideHandlerAuthorizeTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public OverrideHandlerAuthorizeTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {
    }

    [Entity(nameof(OverridedModel))]
    public class OverridedModel : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    //[Entity(nameof(OverridedModel))]
    //[EntityAuthorize]
    //public class QueryOverrideHandler : IEntityQueryHandler<OverridedModel>
    //{
    //    public async Task<Result<IQueryable>> Handle(ODataQueryOptions<OverridedModel> options, CancellationToken cancellationToken)
    //    {
    //        var result = Array.Empty<OverridedModel>().AsQueryable() as IQueryable;
    //        return await Task.FromResult(result.Success());
    //    }
    //}

    [Fact]
    public async Task HasQueryHandlerAuthorize_OtherHandlerShouldOk()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var data = DataGenerator.Create<OverridedModel>();

        // Act
        var createResponse = await httpClient.PostAsJsonAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(OverridedModel)}", data);
        var getByIdResponse = await httpClient.GetAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(OverridedModel)}/{data.Id}");

        var patchData = DataGenerator.Create<OverridedModel>();
        var patchResponse = await httpClient.PatchAsJsonAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(OverridedModel)}/{data.Id}", patchData);
        var deleteResponse = await httpClient.DeleteAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(OverridedModel)}/{data.Id}");

        // Assert
        patchResponse.IsSuccessStatusCode.Should().BeTrue();
        createResponse.IsSuccessStatusCode.Should().BeTrue();
        getByIdResponse.IsSuccessStatusCode.Should().BeTrue();
        deleteResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task HasQueryHandlerAuthorize_QueryShouldUnauthorized()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        // Act
        var queryResponse = await httpClient.GetAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(OverridedModel)}");
        // Assert
        queryResponse.IsSuccessStatusCode.Should().BeFalse();
        queryResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}