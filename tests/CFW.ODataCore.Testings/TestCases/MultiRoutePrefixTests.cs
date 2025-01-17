using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.TestCases;

public class MultiRoutePrefixTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public MultiRoutePrefixTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {
    }

    [Entity(nameof(CustomRoutePrefixModel), RoutePrefix = "custom-prefix")]
    public class CustomRoutePrefixModel : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    [Entity(nameof(DefaultRoutePrefixModel))]
    public class DefaultRoutePrefixModel : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    [Fact]
    public async Task CustomPrefix_Success()
    {
        //Setup
        var customBaseUrl = $"/custom-prefix/{nameof(CustomRoutePrefixModel)}";
        var defaultBaseUrl = $"/{Constants.DefaultODataRoutePrefix}/{nameof(DefaultRoutePrefixModel)}";
        var client = _factory.CreateClient();

        //Act
        var response = await client.GetAsync(customBaseUrl);
        response.IsSuccessStatusCode.Should().BeTrue();

        response = await client.GetAsync(defaultBaseUrl);
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
