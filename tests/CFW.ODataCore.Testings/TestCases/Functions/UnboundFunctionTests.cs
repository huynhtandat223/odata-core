using CFW.Core.Results;
using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.Extensions;
using CFW.ODataCore.Features.BoundOperations;
using Microsoft.AspNetCore.TestHost;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Functions;

public class UnboundFunctionTests : BaseTests, IClassFixture<NonInitAppFactory>
{
    public class Request
    {
        public string TestProperty { get; set; } = string.Empty;
    }

    public class Response
    {
        public int TestProperty { get; set; }
    }

    [UnboundFunction(nameof(UnboundFunctionHandler))]
    public class UnboundFunctionHandler : IUnboundOperationHandler<Request, Response>
    {
        private readonly List<object> _requests;

        public UnboundFunctionHandler(List<object> requests)
        {
            _requests = requests;
        }

        public async Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = new Response { TestProperty = 1 };
            return await Task.FromResult(response.Success());
        }
    }

    private readonly List<object> _requests = new List<object>();

    public UnboundFunctionTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory)
        : base(testOutputHelper, factory)
    {
        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_requests);
                services.AddControllers()
                    .AddGenericODataEndpoints(new TestODataTypeResolver(Constants.DefaultODataRoutePrefix
                    , [typeof(UnboundFunctionHandler)]));
            });
        });
    }

    [Theory]
    [InlineData(typeof(UnboundFunctionHandler))]
    public async Task UnboundFunctionRequest_NonKeyAction_ShouldSuccess(Type actionHandlerType)
    {
        var requestType = actionHandlerType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityOperationHandler<,>))
            .GetGenericArguments().First();
        var request = DataGenerator.Create(requestType);

        var client = _factory.CreateClient();
        var operationUrl = actionHandlerType.GetUnboundFunctionUrl(request);

        var response = await client.GetAsync(operationUrl);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _requests.Should().ContainSingle().Which.Should().BeEquivalentTo(request);
    }
}
