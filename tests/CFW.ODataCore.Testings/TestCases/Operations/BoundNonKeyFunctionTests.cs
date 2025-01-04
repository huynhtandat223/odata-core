namespace CFW.ODataCore.Testings.TestCases.Operations;

public class BoundNonKeyFunctionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public BoundNonKeyFunctionTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Entity(nameof(BoundViewModel))]
    public class BoundViewModel
    {
        public Guid Id { get; set; }
    }

    public class NonKeyFunctionRequest
    {
        public Guid ActionId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class FunctionResponse
    {
        public Guid ActionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }


    [EntityFunction<BoundViewModel>(nameof(NonKeyFunctionHandler))]
    public class NonKeyFunctionHandler :
        IEntityOperationHandler<BoundViewModel, NonKeyFunctionRequest, FunctionResponse>
    {
        private readonly List<object> _requests;
        public NonKeyFunctionHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result<FunctionResponse>> Handle(NonKeyFunctionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = DataGenerator.Create<FunctionResponse>();
            _requests.Add(response);
            return await Task.FromResult(response.Success());
        }
    }


    [EntityFunction<BoundViewModel>(nameof(NonKeyFunctionWithResponseHandler))]
    public class NonKeyFunctionWithResponseHandler :
        IEntityOperationHandler<BoundViewModel, NonKeyFunctionRequest, FunctionResponse>
    {
        private readonly List<object> _requests;
        public NonKeyFunctionWithResponseHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result<FunctionResponse>> Handle(NonKeyFunctionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = DataGenerator.Create<FunctionResponse>();
            _requests.Add(response);
            return await Task.FromResult(response.Success());
        }
    }

    [Fact]
    public async Task BoundFunction_Success()
    {
        // Arrange
        var request = DataGenerator.Create<NonKeyFunctionRequest>();
        var httpClient = _factory.CreateClient();

        // Act

        var requestParams = request.ParseToQueryString();

        var response = await httpClient
            .GetAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(BoundViewModel)}/{nameof(NonKeyFunctionHandler)}?{requestParams}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var handlerRequest = _factory.Server.Services.GetRequiredService<List<object>>().OfType<NonKeyFunctionRequest>().Single();
        handlerRequest.Should().BeEquivalentTo(request);

        var handlerResponse = _factory.Server.Services.GetRequiredService<List<object>>().OfType<FunctionResponse>().Single();
        var responseData = await response.Content.ReadFromJsonAsync<FunctionResponse>();
        handlerResponse.Should().BeEquivalentTo(responseData);
    }

    [Fact]
    public async Task BoundFunction_NonKeyWithResponse_Success()
    {
        //Arrage
        var request = DataGenerator.Create<NonKeyFunctionRequest>();
        var httpClient = _factory.CreateClient();

        //Act
        var requestParams = request.ParseToQueryString();
        var response = await httpClient
            .GetAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(BoundViewModel)}" +
            $"/{nameof(NonKeyFunctionHandler)}?{requestParams}");

        //Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var handlerRequest = _factory.Server.Services.GetRequiredService<List<object>>().OfType<NonKeyFunctionRequest>().Single();
        handlerRequest.Should().BeEquivalentTo(request, o => o.Excluding(x => x.ActionId));


        var handlerResponse = _factory.Server.Services.GetRequiredService<List<object>>().OfType<FunctionResponse>().Single();
        var responseData = await response.Content.ReadFromJsonAsync<FunctionResponse>();
        handlerResponse.Should().BeEquivalentTo(responseData);
    }

    [Fact]
    public async Task BoundFunction_NonKeyWithResponse_EmptyQueryString_HandleWithNullRequest()
    {
        //Arrage
        var httpClient = _factory.CreateClient();

        //Act
        var response = await httpClient
            .GetAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(BoundViewModel)}" +
            $"/{nameof(NonKeyFunctionHandler)}");

        //Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var requests = _factory.Server.Services.GetRequiredService<List<object>>();

        var request = requests.First().Should().BeNull();
        var responseObj = requests.Last().Should().BeOfType<FunctionResponse>();
    }
}
