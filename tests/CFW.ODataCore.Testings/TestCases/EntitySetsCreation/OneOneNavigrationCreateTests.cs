using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.OData;
using CFW.ODataCore.Testings.Models;

namespace CFW.ODataCore.Testings.TestCases.EntitySetsCreation;

public class OneOneNavigrationCreateTests
    : BaseTests, IClassFixture<WebApplicationFactory<Program>>
{
    public OneOneNavigrationCreateTests(ITestOutputHelper testOutputHelper, WebApplicationFactory<Program> factory)
        : base(testOutputHelper, factory)
    {
    }

    public static IEnumerable<object?[]> ResourceWithOneComplexTypeProp =>
            new List<object?[]>
            {
                new object?[] { typeof(Product), nameof(Product.Category), null },
                //new object?[] { typeof(Product), nameof(Product.Category), Guid.NewGuid() }
            };

    [Theory]
    [InlineData(typeof(Product), nameof(Product.Category), null)]
    public async Task Create_MainEntityContainsNewComplexProperty_Should_InsertAllOfResourceSuccess(Type resourceType
        , string complexPropName, object? complexPropValue)
    {
        // Arrange
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);
        var baseUrl = resourceType.GetBaseUrl();

        // Act
        var expectedEntity = DataGenerator.Create(resourceType);
        var navigationProp = expectedEntity.GetPropertyValue(complexPropName);
        navigationProp.SetPropertyValue(idProp, complexPropValue);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var actual = await resp.Content.ReadFromJsonAsync(resourceType);

        var dbEntity = await client.GetFromJsonAsync($"{baseUrl}/{actual!.GetPropertyValue(idProp)}?$expand={complexPropName}", resourceType);

        // Assert
        expectedEntity.Should().BeEquivalentTo(dbEntity, o => o.Excluding(x => x.Name == idProp));

        //compare complex property
        var childProp = dbEntity!.GetPropertyValue(complexPropName);
        childProp.Should().NotBeNull();
        var idPropValue = childProp!.GetPropertyValue(idProp);
        var navigationPropType = navigationProp!.GetType();
        var complexPropUrl = navigationPropType.GetBaseUrl();
        var childPropEntity = await client.GetFromJsonAsync($"{complexPropUrl}/{idPropValue}", navigationPropType);
        childPropEntity.Should().BeEquivalentTo(navigationProp, o => o.Excluding(x => x.Name == idProp));
    }

    [Theory]
    [InlineData(typeof(Product), nameof(Product.Category))]
    public async Task Create_MainEntityContainsNewComplexPropertyThatHasPrimaryKey_Should_InsertSuccess(Type resourceType
        , string complexPropName)
    {
        // Arrange
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);
        var baseUrl = resourceType.GetBaseUrl();

        // Act
        var expectedEntity = DataGenerator.Create(resourceType);
        var navigationProp = expectedEntity.GetPropertyValue(complexPropName);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);
        resp.IsSuccessStatusCode.Should().BeTrue();

        var actual = await resp.Content.ReadFromJsonAsync(resourceType);

        // Assert
        var dbEntity = await client.GetFromJsonAsync($"{baseUrl}/{actual!.GetPropertyValue(idProp)}?$expand={complexPropName}", resourceType);
        expectedEntity.Should().BeEquivalentTo(dbEntity, o => o.Excluding(x => x.Name == idProp));

        //compare complex property
        var childProp = dbEntity!.GetPropertyValue(complexPropName);
        childProp.Should().NotBeNull();
        var idPropValue = childProp!.GetPropertyValue(idProp);
        var navigationPropType = navigationProp!.GetType();
        var complexPropUrl = navigationPropType.GetBaseUrl();
        var childPropEntity = await client.GetFromJsonAsync($"{complexPropUrl}/{idPropValue}", navigationPropType);
        childPropEntity.Should().BeEquivalentTo(navigationProp, o => o.Excluding(x => x.Name == idProp));
    }

    [Theory]
    [InlineData(typeof(Product), nameof(Product.Category))]
    public async Task Create_MainEntityContainsExistingComplexProperty_Should_InsertOnlyMainEntitySuccess(Type resourceType
        , string complexPropName)
    {
        // Arrange
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);
        var baseUrl = resourceType.GetBaseUrl();

        //Create complex property
        var complexPropType = resourceType.GetProperty(complexPropName)!.PropertyType;
        var complexPropUrl = complexPropType.GetBaseUrl();
        var complexProp = DataGenerator.Create(complexPropType);
        var dbComplexPropValueResp = await client.PostAsJsonAsync(complexPropUrl, complexProp);
        dbComplexPropValueResp.IsSuccessStatusCode.Should().BeTrue();
        var dbComplexPropValue = await dbComplexPropValueResp.Content.ReadFromJsonAsync(complexPropType);
        dbComplexPropValue.Should().NotBeNull();

        // Act
        var expectedEntity = DataGenerator.Create(resourceType)
            .SetPropertyValue(complexPropName, dbComplexPropValue);
        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);

        // Assert
        resp.IsSuccessStatusCode.Should().BeTrue();

        //compare main entity
        var actual = await resp.Content.ReadFromJsonAsync(resourceType);
        var dbEntity = await client.GetFromJsonAsync($"{baseUrl}/{actual!.GetPropertyValue(idProp)}?$expand={complexPropName}", resourceType);
        dbEntity.Should().NotBeNull();
        expectedEntity.Should().BeEquivalentTo(dbEntity);

        //compare complex property
        var childProp = dbEntity!.GetPropertyValue(complexPropName);
        childProp.Should().NotBeNull();
        var idPropValue = childProp!.GetPropertyValue(idProp);
        var childPropEntity = await client.GetFromJsonAsync($"{complexPropUrl}/{idPropValue}", complexPropType);
        childPropEntity.Should().BeEquivalentTo(dbComplexPropValue);
    }
}
