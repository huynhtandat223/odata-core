using CFW.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Operations;

public class BoundKeyedFunctionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public BoundKeyedFunctionTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Entity(nameof(BoundKeyedFunctionViewModel))]
    public class BoundKeyedFunctionViewModel : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public class KeyedFunctionRequest
    {
        [FromRoute]
        public Guid ActionId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class KeyedFunctionResponse
    {
        public Guid ActionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [EntityFunction<BoundKeyedFunctionViewModel>(nameof(KeyedFunctionWithResponseHandler))]
    public class KeyedFunctionWithResponseHandler :
        IEntityOperationHandler<BoundKeyedFunctionViewModel, KeyedFunctionRequest, KeyedFunctionResponse>
    {
        private readonly List<object> _requests;
        public KeyedFunctionWithResponseHandler(List<object> requests)
        {
            _requests = requests;
        }
        public async Task<Result<KeyedFunctionResponse>> Handle(KeyedFunctionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = DataGenerator.Create<KeyedFunctionResponse>();
            _requests.Add(response);
            return await Task.FromResult(response.Success());
        }
    }

    [Fact]
    public async Task Execute_BoundKeyedFunction_WithResponseData_Success()
    {
        // Arrange
        var request = DataGenerator.Create<KeyedFunctionRequest>();
        var requestParams = request.ParseToQueryString();
        var httpClient = _factory.CreateClient();
        var id = Guid.NewGuid();

        // Act
        var response = await httpClient
            .GetAsync($"{Constants.DefaultODataRoutePrefix}/{nameof(BoundKeyedFunctionViewModel)}/{id}" +
            $"/{nameof(KeyedFunctionWithResponseHandler)}?{requestParams}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var requests = _factory.Services.GetRequiredService<List<object>>();
        var actualRequest = requests.OfType<KeyedFunctionRequest>().Single();
        actualRequest.Should().BeEquivalentTo(request, o => o.Excluding(x => x.ActionId));
        actualRequest.ActionId.Should().Be(id);

        var actualResponse = requests.OfType<KeyedFunctionResponse>().Single();
        var responseData = await response.Content.ReadFromJsonAsync<KeyedFunctionResponse>();
        responseData.Should().BeEquivalentTo(actualResponse);
    }
}

