using CFW.Core.Entities;
using CFW.Core.Results;
using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.Extensions;
using Microsoft.AspNetCore.TestHost;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Actions;

public class NonKeyBoundActionTests : BaseTests, IClassFixture<NonInitAppFactory>
{
    [ODataEntitySet(nameof(NonKeyBoundActionViewModel))]
    public class NonKeyBoundActionViewModel : IODataViewModel<Guid>, IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public class NonKeyActionRequest
    {
        public Guid ActionId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [BoundAction<NonKeyBoundActionViewModel, Guid>(nameof(NonKeyActionHandler))]
    public class NonKeyActionHandler : IODataActionHandler<NonKeyActionRequest>
    {
        private readonly List<object> _requests;

        public NonKeyActionHandler(List<object> requests)
        {
            _requests = requests;
        }

        public async Task<Result> Execute(NonKeyActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return await Task.FromResult(this.Success());
        }
    }


    private readonly List<object> _requests = new List<object>();

    public NonKeyBoundActionTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory) : base(testOutputHelper, factory)
    {
        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_requests);
                services.AddControllers()
                    .AddGenericODataEndpoints(new TestODataTypeResolver(Constants.DefaultODataRoutePrefix
                    , [typeof(NonKeyBoundActionViewModel), typeof(NonKeyActionHandler)]));
            });
        });
    }

    [Theory]
    [InlineData(typeof(NonKeyBoundActionViewModel), typeof(NonKeyActionHandler))]
    public async Task Request_NonKeyAction_ShouldSuccess(Type resourceType, Type actionHandlerType)
    {
        var requestType = actionHandlerType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IODataActionHandler<>))
            .GetGenericArguments().First();
        var request = DataGenerator.Create(requestType);

        var client = _factory.CreateClient();
        var actionUrl = TestUtils.GetNonKeyActionUrl(resourceType, actionHandlerType);

        var response = await client.PostAsJsonAsync(actionUrl, request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _requests.Should().ContainSingle().Which.Should().BeEquivalentTo(request);
    }

    [Theory]
    [InlineData(typeof(NonKeyBoundActionViewModel), typeof(NonKeyActionHandler), "Body")]
    [InlineData(typeof(NonKeyBoundActionViewModel), typeof(NonKeyActionHandler), "body")]
    [InlineData(typeof(NonKeyBoundActionViewModel), typeof(NonKeyActionHandler), "boDY")]
    public async Task Request_NonKeyAction_WrapBody_ShouldSuccess(Type resourceType, Type actionHandlerType, string bodyPropValue)
    {
        var client = _factory.CreateClient();
        var requestType = actionHandlerType.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IODataActionHandler<>))
                    .GetGenericArguments().First();
        var request = DataGenerator.Create(requestType);

        var actionUrl = TestUtils.GetNonKeyActionUrl(resourceType, actionHandlerType);

        var requestBody = new Dictionary<string, object?>
        {
            [bodyPropValue] = request
        };
        var response = await client.PostAsJsonAsync(actionUrl, requestBody);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _requests.Should().ContainSingle().Which.Should().BeEquivalentTo(request);
    }
}
