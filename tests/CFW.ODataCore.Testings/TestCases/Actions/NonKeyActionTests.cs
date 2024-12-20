using CFW.Core.Entities;
using CFW.Core.Results;
using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.Extensions;
using CFW.ODataCore.Features.BoundActions;
using CFW.ODataCore.OData;
using Microsoft.AspNetCore.TestHost;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.Actions;

[ODataRouting("non-key-actions")]
public class NonKeyActionViewModel : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }
}

public class ActionRequest
{
    public Guid ActionId { get; set; }

    public string Name { get; set; } = string.Empty;
}

[BoundAction<NonKeyActionViewModel, Guid>("test")]
public class NonKeyActionHandler : IODataActionHandler<ActionRequest>
{
    public async Task<Result> Execute(ActionRequest request, CancellationToken cancellationToken)
    {
        return await Task.FromResult(this.Success());
    }
}


public class NonKeyActionTests : BaseTests, IClassFixture<NonInitAppFactory>
{
    public NonKeyActionTests(ITestOutputHelper testOutputHelper, NonInitAppFactory factory) : base(testOutputHelper, factory)
    {
        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers()
                    .AddGenericODataEndpoints(new TestODataTypeResolver(Constants.DefaultODataRoutePrefix
                    , [typeof(NonKeyActionViewModel), typeof(NonKeyActionHandler)]));
            });
        });
    }

    [Fact]
    public async Task Request_NonKeyAction_ShouldSuccess()
    {
        var client = _factory.CreateClient();
        var request = DataGenerator.Create<ActionRequest>();
        var response = await client.PostAsJsonAsync($"{Constants.DefaultODataRoutePrefix}/non-key-actions/test", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_NonKeyAction_WrapBody_ShouldSuccess()
    {
        var client = _factory.CreateClient();
        var request = DataGenerator.Create<ActionRequest>();
        var response = await client.PostAsJsonAsync($"{Constants.DefaultODataRoutePrefix}/non-key-actions/test", new
        {
            Body = request
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
