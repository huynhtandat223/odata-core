using CFW.ODataCore.Testings.Features.Categories;
using CFW.ODataCore.Testings.Models;

namespace CFW.ODataCore.Testings.TestCases;

public class BoundKeyedActionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public BoundKeyedActionTests(ITestOutputHelper testOutputHelper
        , AppFactory factory) : base(testOutputHelper, factory, types: [typeof(Category)
            , typeof(CategoriesPingPongWithKey.Handler)])
    {
    }

    [Theory]
    [InlineData(typeof(CategoriesPingPongWithKey.Handler)
        , typeof(CategoriesPingPongWithKey.RequestPing)
        , typeof(CategoriesPingPongWithKey.ResponsePong))]
    public async Task Execute_BoundKeyedAction_DefaultPostMethod_EntityMultipleEndpoint_Success(Type handlerType
        , Type requestType, Type responseType)
    {
        // Arrange
        var request = DataGenerator.Create(requestType);
        var key = request.GetPropertyValue(nameof(CategoriesPingPongWithKey.RequestPing.RequestId));
        var (baseUrl, attr) = handlerType.GetKeyedActionUrl(key!);

        request.SetPropertyValue(nameof(CategoriesPingPongWithKey.RequestPing.RequestId), null);

        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync(baseUrl, request);

        // Assert
        response.Should().BeSuccessful();
        var responseData = response.GetResponseResult(responseType);
        request.SetPropertyValue(nameof(CategoriesPingPongWithKey.RequestPing.RequestId), key);
        responseData.Should().BeEquivalentTo(request);
    }
}
