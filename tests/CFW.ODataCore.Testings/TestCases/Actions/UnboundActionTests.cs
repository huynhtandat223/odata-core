
using CFW.Core.Results;
using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.Extensions;
using CFW.ODataCore.Features.Shared;
using CFW.ODataCore.Features.UnBoundActions;
using Microsoft.AspNetCore.TestHost;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Actions;

public class UnboundActionTests : BaseTests, IClassFixture<NonInitAppFactory>
{
    public class UnboundActionRequest
    {
        public string TestProperty { get; set; } = string.Empty;
    }

    [UnboundAction(nameof(UnboundActionHandler))]
    public class UnboundActionHandler : IODataActionHandler<UnboundActionRequest>
    {
        private readonly List<object> _requests;

        public UnboundActionHandler(List<object> requests)
        {
            _requests = requests;
        }

        public async Task<Result> Execute(UnboundActionRequest request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return await Task.FromResult(this.Success());
        }
    }

    private readonly List<object> _requests = new List<object>();

    public UnboundActionTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory)
        : base(testOutputHelper, factory)
    {
        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_requests);
                services.AddControllers()
                    .AddGenericODataEndpoints(new TestODataTypeResolver(Constants.DefaultODataRoutePrefix
                    , [typeof(UnboundActionHandler)]));
            });
        });
    }

    [Theory]
    [InlineData(typeof(UnboundActionHandler))]
    public async Task Request_NonKeyAction_ShouldSuccess(Type actionHandlerType)
    {
        var requestType = actionHandlerType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IODataActionHandler<>))
            .GetGenericArguments().First();
        var request = DataGenerator.Create(requestType);

        var client = _factory.CreateClient();
        var actionUrl = actionHandlerType.GetUnboundActionUrl();

        var response = await client.PostAsJsonAsync(actionUrl, request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _requests.Should().ContainSingle().Which.Should().BeEquivalentTo(request);
    }
}
