using CFW.Core.Entities;
using CFW.Core.Results;
using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.Extensions;
using Microsoft.AspNetCore.TestHost;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Functions;

public class NonKeyBoundFunctionTests : BaseTests, IClassFixture<NonInitAppFactory>
{
    [ODataEntitySet(nameof(NonKeyBoundFunctionViewModel))]
    public class NonKeyBoundFunctionViewModel : IODataViewModel<Guid>, IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public class Request
    {
        public Guid ActionId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class Response
    {
        public Guid ResponseId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [BoundFunction<NonKeyBoundFunctionViewModel, Guid>(nameof(NonKeyFunctionHandler))]
    public class NonKeyFunctionHandler : IODataOperationHandler<Request, Response>
    {
        private readonly List<object> _requests;

        public NonKeyFunctionHandler(List<object> requests)
        {
            _requests = requests;
        }

        public async Task<Result<Response>> Execute(Request request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            var response = new Response
            {
                ResponseId = request.ActionId,
                Name = request.Name
            };
            return await Task.FromResult(response.Success());
        }
    }


    private readonly List<object> _requests = new List<object>();

    public NonKeyBoundFunctionTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory) : base(testOutputHelper, factory)
    {
        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_requests);
                services.AddControllers()
                    .AddGenericODataEndpoints(new TestODataTypeResolver(Constants.DefaultODataRoutePrefix
                    , [typeof(NonKeyBoundFunctionViewModel), typeof(NonKeyFunctionHandler)]));
            });
        });
    }

    [Theory]
    [InlineData(typeof(NonKeyBoundFunctionViewModel), typeof(NonKeyFunctionHandler))]
    public async Task Request_NonKeyFunction_ShouldSuccess(Type resourceType, Type actionHandlerType)
    {
        var requestType = actionHandlerType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IODataOperationHandler<,>))
            .GetGenericArguments().First();
        var request = DataGenerator.Create(requestType);

        var client = _factory.CreateClient();
        var actionUrl = resourceType.GetNonKeyFunctionUrl(actionHandlerType, request);

        var response = await client.GetAsync(actionUrl);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _requests.Should().ContainSingle().Which.Should().BeEquivalentTo(request);
    }

}
