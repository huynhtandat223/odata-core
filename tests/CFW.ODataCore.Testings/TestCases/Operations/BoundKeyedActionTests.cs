
using CFW.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Operations;

public class BoundKeyedActionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public BoundKeyedActionTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Entity(nameof(BoundKeyedViewModel))]
    public class BoundKeyedViewModel : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public class KeyedActionRequest
    {
        [FromRoute]
        public Guid Id { get; set; }

        public Guid ActionId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class KeyedActionResponse
    {
        public Guid ActionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [EntityAction<BoundKeyedViewModel>(nameof(KeyedActionHandler))]
    public class KeyedActionHandler :
        IEntityOperationHandler<BoundKeyedViewModel, KeyedActionRequest>
    {
        private readonly List<object> _requests;
        public KeyedActionHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result> Handle(KeyedActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return await Task.FromResult(this.Success());
        }
    }

    [EntityAction<BoundKeyedViewModel>(nameof(KeyedActionWithResponseHandler))]
    public class KeyedActionWithResponseHandler :
        IEntityOperationHandler<BoundKeyedViewModel, KeyedActionRequest, KeyedActionResponse>
    {
        private readonly List<object> _requests;
        public KeyedActionWithResponseHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result<KeyedActionResponse>> Handle(KeyedActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = DataGenerator.Create<KeyedActionResponse>();
            _requests.Add(response);
            return await Task.FromResult(response.Success());
        }
    }

    [Fact]
    public async Task Execute_BoundKeyedAction_NoResponseData_Success()
    {
        // Arrange
        var request = DataGenerator.Create<KeyedActionRequest>();
        var httpClient = _factory.CreateClient();
        var id = Guid.NewGuid();

        // Act
        var response = await httpClient.PostAsJsonAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(BoundKeyedViewModel)}/{id}/{nameof(KeyedActionHandler)}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var requests = _factory.Services.GetRequiredService<List<object>>();
        requests.Should().ContainSingle();

        var actualRequest = requests.OfType<KeyedActionRequest>().Single();
        actualRequest.Should().BeEquivalentTo(request, o => o.Excluding(x => x.Id));
        actualRequest.Id.Should().Be(id);
    }

    [Fact]
    public async Task Execute_BoundKeyedAction_WithResponseData_Success()
    {
        // Arrange
        var request = DataGenerator.Create<KeyedActionRequest>();
        var httpClient = _factory.CreateClient();
        var id = Guid.NewGuid();

        // Act
        var response = await httpClient.PostAsJsonAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(BoundKeyedViewModel)}/{id}/{nameof(KeyedActionWithResponseHandler)}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var requests = _factory.Services.GetRequiredService<List<object>>();
        var actualRequest = requests.OfType<KeyedActionRequest>().Single();
        actualRequest.Should().BeEquivalentTo(request, o => o.Excluding(x => x.Id));
        actualRequest.Id.Should().Be(id);

        var actualResponse = requests.OfType<KeyedActionResponse>().Single();
        var responseData = await response.Content.ReadFromJsonAsync<KeyedActionResponse>();
        responseData.Should().BeEquivalentTo(actualResponse);
    }
}
