using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.Testings.TestCases.Operations;

public class UnboundNonKeyFunctionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public UnboundNonKeyFunctionTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    public class UnboundNonKeyFunctionRequest
    {
        public Guid ActionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UnboundNonKeyFunctionResponse
    {
        public Guid ActionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UnboundKeyedFunctionRequest
    {
        [FromRoute]
        public Guid ActionId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class UnboundKeyedFunctionResponse
    {
        public Guid ActionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }


    [UnboundFunction(nameof(UnboundNonKeyFunctionHandler))]
    public class UnboundNonKeyFunctionHandler : IUnboundOperationHandler<UnboundNonKeyFunctionRequest
        , UnboundNonKeyFunctionResponse>
    {
        private readonly List<object> _requests;
        public UnboundNonKeyFunctionHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result<UnboundNonKeyFunctionResponse>> Handle(UnboundNonKeyFunctionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = DataGenerator.Create<UnboundNonKeyFunctionResponse>();
            _requests.Add(response);
            return await Task.FromResult(response.Success());
        }
    }

    [UnboundFunction(nameof(UnboundKeyedFunctionHandler))]
    public class UnboundKeyedFunctionHandler : IUnboundOperationHandler<UnboundKeyedFunctionRequest
        , UnboundKeyedFunctionResponse>
    {
        private readonly List<object> _requests;
        public UnboundKeyedFunctionHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result<UnboundKeyedFunctionResponse>> Handle(UnboundKeyedFunctionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = DataGenerator.Create<UnboundKeyedFunctionResponse>();
            _requests.Add(response);
            return await Task.FromResult(response.Success());
        }
    }

    [Fact]
    public async Task UnboundNonKeyFunction_Success()
    {
        // Arrange
        var request = DataGenerator.Create<UnboundNonKeyFunctionRequest>();
        var requestParams = request.ParseToQueryString();
        var client = _factory.CreateClient();

        // Act
        var response = await client
            .GetFromJsonAsync<UnboundNonKeyFunctionResponse>($"/{Constants.DefaultODataRoutePrefix}" +
            $"/{nameof(UnboundNonKeyFunctionHandler)}?{requestParams}");

        // Assert
        response.Should().NotBeNull();
        var handlerRequest = _factory.Server.Services.GetRequiredService<List<object>>()
            .OfType<UnboundNonKeyFunctionRequest>().Single();
        handlerRequest.Should().BeEquivalentTo(request);
    }

    [Fact]
    public async Task UnboundKeyedFunction_Success()
    {
        // Arrange
        var request = DataGenerator.Create<UnboundKeyedFunctionRequest>();
        var id = Guid.NewGuid();
        var requestParams = request.ParseToQueryString();
        var client = _factory.CreateClient();

        // Act
        var response = await client
            .GetFromJsonAsync<UnboundKeyedFunctionResponse>($"/{Constants.DefaultODataRoutePrefix}" +
            $"/{nameof(UnboundKeyedFunctionHandler)}/{id}?{requestParams}");

        // Assert
        response.Should().NotBeNull();
        var handlerRequests = _factory.Server.Services.GetRequiredService<List<object>>();

        var handlerRequest = handlerRequests.OfType<UnboundKeyedFunctionRequest>().Single();
        handlerRequest.Should().BeEquivalentTo(request, o => o.Excluding(e => e.ActionId));

        handlerRequest.ActionId.Should().Be(id);

        var handlerResponse = handlerRequests.OfType<UnboundKeyedFunctionResponse>().Single();
        response.Should().BeEquivalentTo(handlerResponse);

    }
}