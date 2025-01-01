using CFW.Core.Results;
using CFW.ODataCore.Intefaces;

namespace CFW.ODataCore.Testings.TestCases.Operations;

public class BoundNoneKeyActionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public BoundNoneKeyActionTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Entity(nameof(BoundNonkeyViewModel))]
    public class BoundNonkeyViewModel
    {
        public Guid Id { get; set; }
    }

    public class NonKeyActionRequest
    {
        public Guid ActionId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class NonKeyActionResponse
    {
        public Guid ActionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [EntityAction<BoundNonkeyViewModel>(nameof(KeyedActionHandler))]
    public class KeyedActionHandler :
        IEntityOperationHandler<BoundNonkeyViewModel, NonKeyActionRequest>
    {
        private readonly List<object> _requests;

        public KeyedActionHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result> Handle(NonKeyActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return await Task.FromResult(this.Success());
        }
    }

    [EntityAction<BoundNonkeyViewModel>(nameof(KeyedActionWithResponseHandler))]
    public class KeyedActionWithResponseHandler :
        IEntityOperationHandler<BoundNonkeyViewModel, NonKeyActionRequest, NonKeyActionResponse>
    {
        private readonly List<object> _requests;

        public KeyedActionWithResponseHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result<NonKeyActionResponse>> Handle(NonKeyActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = DataGenerator.Create<NonKeyActionResponse>();
            _requests.Add(response);
            return await Task.FromResult(response.Success());
        }
    }

    [Fact]
    public async Task Execute_BoundNoneKeyAction_NoResponseData_Success()
    {
        // Arrange
        var request = DataGenerator.Create<NonKeyActionRequest>();

        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(BoundNonkeyViewModel)}/{nameof(KeyedActionHandler)}", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var handlerRequest = _factory.Server.Services.GetRequiredService<List<object>>().OfType<NonKeyActionRequest>().FirstOrDefault();
        handlerRequest.Should().NotBeNull();
        handlerRequest.Should().BeEquivalentTo(request);

    }

    [Fact]
    public async Task Execute_BoundNoneKeyAction_WithResponseData_Success()
    {
        // Arrange
        var request = DataGenerator.Create<NonKeyActionRequest>();

        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync($"/{Constants.DefaultODataRoutePrefix}/{nameof(BoundNonkeyViewModel)}/{nameof(KeyedActionWithResponseHandler)}"
            , request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var handlerRequest = _factory.Server.Services.GetRequiredService<List<object>>().OfType<NonKeyActionRequest>().FirstOrDefault();
        handlerRequest.Should().NotBeNull();
        handlerRequest.Should().BeEquivalentTo(request);

        var handlerResponse = _factory.Server.Services.GetRequiredService<List<object>>().OfType<NonKeyActionResponse>().FirstOrDefault();
        handlerResponse.Should().NotBeNull();

        var responseData = await response.Content.ReadFromJsonAsync<NonKeyActionResponse>();
        handlerResponse.Should().BeEquivalentTo(responseData);
    }
}
