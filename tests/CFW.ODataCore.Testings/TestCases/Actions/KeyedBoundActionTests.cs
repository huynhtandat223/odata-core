using CFW.Core.Entities;
using CFW.Core.Results;
using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Actions;

public class KeyedBoundActionTests : BaseTests, IClassFixture<NonInitAppFactory>
{

    [ODataEntitySet(nameof(KeyedBoundActionViewModel))]
    public class KeyedBoundActionViewModel : IODataViewModel<Guid>, IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public class KeyedActionRequest
    {
        [FromRoute]
        public Guid Id { set; get; }

        public Guid ActionId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [BoundAction<KeyedBoundActionViewModel, Guid>(nameof(KeyedActionHandler))]
    public class KeyedActionHandler : IODataOperationHandler<KeyedActionRequest>
    {
        private readonly List<object> _requests;
        public KeyedActionHandler(List<object> requests)
        {
            _requests = requests;
        }

        public async Task<Result> Execute(KeyedActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return await Task.FromResult(this.Success());
        }
    }



    private readonly List<object> _requests = new List<object>();
    public KeyedBoundActionTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory) : base(testOutputHelper, factory)
    {
        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_requests);
                services.AddControllers()
                    .AddGenericODataEndpoints(new TestODataTypeResolver(Constants.DefaultODataRoutePrefix
                    , [typeof(KeyedBoundActionViewModel), typeof(KeyedActionHandler)]));
            });
        });
    }

    [Theory]
    [InlineData(typeof(KeyedBoundActionViewModel), typeof(KeyedActionHandler))]
    public async Task Request_KeyedAction_ShouldSuccess(Type resourceType, Type actionHandlerType)
    {
        var requestType = actionHandlerType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IODataOperationHandler<>))
            .GetGenericArguments().First();
        var request = DataGenerator.Create(requestType);
        var routeId = Guid.NewGuid();

        var client = _factory.CreateClient();
        var actionUrls = TestUtils.GetKeyedActionUrl(resourceType, actionHandlerType, routeId);
        foreach (var actionUrl in actionUrls)
        {
            var response = await client.PostAsJsonAsync(actionUrl, request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _requests.Should().ContainSingle().Which.Should().BeEquivalentTo(request);
            _requests.Clear();
        }
    }

    [Theory]
    [InlineData(typeof(KeyedBoundActionViewModel), typeof(KeyedActionHandler), "Body")]
    [InlineData(typeof(KeyedBoundActionViewModel), typeof(KeyedActionHandler), "body")]
    [InlineData(typeof(KeyedBoundActionViewModel), typeof(KeyedActionHandler), "boDY")]
    public async Task Request_NonKeyAction_WrapBody_ShouldSuccess(Type resourceType, Type actionHandlerType, string bodyPropValue)
    {
        var requestType = actionHandlerType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IODataOperationHandler<>))
            .GetGenericArguments().First();
        var routeId = Guid.NewGuid();

        var client = _factory.CreateClient();
        var actionUrls = TestUtils.GetKeyedActionUrl(resourceType, actionHandlerType, routeId);
        foreach (var actionUrl in actionUrls)
        {
            var request = DataGenerator.Create(requestType);
            var requestBody = new Dictionary<string, object?>
            {
                [bodyPropValue] = request
            };

            var response = await client.PostAsJsonAsync(actionUrl, requestBody);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _requests.Should().ContainSingle().Which.Should().BeEquivalentTo(request);
            _requests.Clear();
        }
    }
}

