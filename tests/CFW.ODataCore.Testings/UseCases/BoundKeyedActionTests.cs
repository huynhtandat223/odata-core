using CFW.ODataCore.Testings.Features.Categories;
using CFW.ODataCore.Testings.Models;
using CFW.ODataCore.Testings.TestCases;

namespace CFW.ODataCore.Testings.UseCases;

public class BoundKeyedActionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public BoundKeyedActionTests(ITestOutputHelper testOutputHelper
        , AppFactory factory) : base(testOutputHelper, factory, types: [typeof(Category)
            , typeof(CategoriesPingPongWithKeyAttr.Handler)])
    {
    }

    [Theory]
    [InlineData(typeof(CategoriesPingPongWithKeyAttr.Handler)
        , typeof(CategoriesPingPongWithKeyAttr.RequestPing)
        , typeof(CategoriesPingPongWithKeyAttr.ResponsePong))]
    public async Task Execute_BoundKeyedAction_DefaultPostMethod_EntityMultipleEndpoint_Success(Type handlerType
        , Type requestType, Type responseType)
    {
        // Arrange
        var request = DataGenerator.Create(requestType);
        var key = request.GetPropertyValue(nameof(CategoriesPingPongWithKeyAttr.RequestPing.RequestId));
        var (baseUrl, attr) = handlerType.GetKeyedActionUrl(key!);

        request.SetPropertyValue(nameof(CategoriesPingPongWithKeyAttr.RequestPing.RequestId), null);

        var client = _factory.CreateClient();

        // Act
        var response = await client
            .PostAsJsonAsync(baseUrl, request);

        // Assert
        response.Should().BeSuccessful();
        var responseData = response.GetResponseResult(responseType);
        request.SetPropertyValue(nameof(CategoriesPingPongWithKeyAttr.RequestPing.RequestId), key);
        responseData.Should().BeEquivalentTo(request);
    }
}
