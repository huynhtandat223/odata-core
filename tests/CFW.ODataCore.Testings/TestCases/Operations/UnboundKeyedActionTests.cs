using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.Testings.TestCases.Operations;

public class UnboundKeyedActionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public UnboundKeyedActionTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }


    public class UnboundKeyedActionRequest
    {
        [FromRoute]
        public Guid Id { get; set; }
        public Guid ActionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UnboundKeyedActionResponse
    {
        public Guid ActionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [UnboundAction(nameof(UnboundKeyedActionHandler))]
    public class UnboundKeyedActionHandler : IUnboundOperationHandler<UnboundKeyedActionRequest>
    {
        private readonly List<object> _requests;
        public UnboundKeyedActionHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result> Handle(UnboundKeyedActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return await Task.FromResult(this.Success());
        }
    }

    [Fact]
    public async Task UnboundKeyedAction_NoResponse_Success()
    {
        // Arrange
        var request = DataGenerator.Create<UnboundKeyedActionRequest>();
        var id = Guid.NewGuid();
        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(UnboundKeyedActionHandler)}/{id}", request);
        // Assert

        response.IsSuccessStatusCode.Should().BeTrue();
        var handlerRequest = _factory.Server.Services.GetRequiredService<List<object>>()
            .OfType<UnboundKeyedActionRequest>().Single();
        handlerRequest.Should().BeEquivalentTo(request, o => o.Excluding(e => e.Id));

        handlerRequest.Id.Should().Be(id);
    }

    [UnboundAction(nameof(UnboundKeyedActionWithResponseHandler))]
    public class UnboundKeyedActionWithResponseHandler : IUnboundOperationHandler<UnboundKeyedActionRequest, UnboundKeyedActionResponse>
    {
        private readonly List<object> _requests;
        public UnboundKeyedActionWithResponseHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result<UnboundKeyedActionResponse>> Handle(UnboundKeyedActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = DataGenerator.Create<UnboundKeyedActionResponse>();
            _requests.Add(response);
            return await Task.FromResult(response.Success());
        }
    }

    [Fact]
    public async Task UnboundKeyedAction_WithResponse_Success()
    {
        // Arrange
        var request = DataGenerator.Create<UnboundKeyedActionRequest>();
        var id = Guid.NewGuid();
        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(UnboundKeyedActionWithResponseHandler)}/{id}", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var requests = _factory.Server.Services.GetRequiredService<List<object>>();

        var handlerRequest = requests.OfType<UnboundKeyedActionRequest>().Single();
        handlerRequest.Should().BeEquivalentTo(request, o => o.Excluding(e => e.Id));
        handlerRequest.Id.Should().Be(id);

        var handlerResponse = requests.OfType<UnboundKeyedActionResponse>().Single();
        var responseData = await response.Content.ReadFromJsonAsync<UnboundKeyedActionResponse>();
        responseData.Should().BeEquivalentTo(handlerResponse);
    }
}
