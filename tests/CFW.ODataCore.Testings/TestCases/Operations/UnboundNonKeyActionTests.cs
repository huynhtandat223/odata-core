
namespace CFW.ODataCore.Testings.TestCases.Operations;

public class UnboundNonKeyActionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public UnboundNonKeyActionTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    public class UnboundNonKeyActionRequest
    {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }

    public class UnboundNonKeyActionResponse
    {
        public string ResponseName { get; set; } = string.Empty;
        public int ResponseAge { get; set; }
    }

    [UnboundAction(nameof(UnboundNonKeyNoResponseActionHandler))]
    public class UnboundNonKeyNoResponseActionHandler : IUnboundOperationHandler<UnboundNonKeyActionRequest>
    {
        private readonly List<object> _requests;
        public UnboundNonKeyNoResponseActionHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result> Handle(UnboundNonKeyActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return await Task.FromResult(this.Success());
        }
    }

    [Fact]
    public async Task UnboundNonKeyAction_NoResponse_Success()
    {
        // Arrange
        var request = DataGenerator.Create<UnboundNonKeyActionRequest>();

        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(UnboundNonKeyNoResponseActionHandler)}", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var handlerRequest = _factory.Server.Services.GetRequiredService<List<object>>()
            .OfType<UnboundNonKeyActionRequest>().Single();

        handlerRequest.Should().BeEquivalentTo(request);
    }

    [UnboundAction(nameof(UnboundNonKeyHasResponseActionHandler))]
    public class UnboundNonKeyHasResponseActionHandler : IUnboundOperationHandler<UnboundNonKeyActionRequest, UnboundNonKeyActionResponse>
    {
        private readonly List<object> _requests;
        public UnboundNonKeyHasResponseActionHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result<UnboundNonKeyActionResponse>> Handle(UnboundNonKeyActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = DataGenerator.Create<UnboundNonKeyActionResponse>();
            _requests.Add(response);
            return await Task.FromResult(response.Success());
        }
    }

    [Fact]
    public async Task Execute_UnBoundNoneKeyAction_WithResponseData_Success()
    {
        // Arrange
        var request = DataGenerator.Create<UnboundNonKeyActionRequest>();

        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(UnboundNonKeyHasResponseActionHandler)}"
            , request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var handlerRequest = _factory.Server.Services.GetRequiredService<List<object>>()
            .OfType<UnboundNonKeyActionRequest>().FirstOrDefault();
        handlerRequest.Should().NotBeNull();
        handlerRequest.Should().BeEquivalentTo(request);

        var handlerResponse = _factory.Server.Services.GetRequiredService<List<object>>()
            .OfType<UnboundNonKeyActionResponse>().FirstOrDefault();
        handlerResponse.Should().NotBeNull();

        var responseData = await response.Content.ReadFromJsonAsync<UnboundNonKeyActionResponse>();
        handlerResponse.Should().BeEquivalentTo(responseData);
    }
}
