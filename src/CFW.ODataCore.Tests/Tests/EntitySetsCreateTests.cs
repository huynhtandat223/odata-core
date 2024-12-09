using CFW.Core.Testings.DataGenerations;
using CFW.Core.Utils;
using CFW.ODataCore.Core;
using CFW.ODataCore.Tests.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CFW.ODataCore.Tests.Tests;

public class EntitySetsCreateTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EntitySetsCreateTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("odata-api/categories", typeof(Category), null)]
    [InlineData("odata-api/customers", typeof(Customer), 0)]
    public async Task Create_NoCollectionProperty_ShouldSuccess(string baseUrl, Type resourceType, object? idValue)
    {
        // Arrange
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);

        // Act
        var expectedEntity = DataGenerator.Create(resourceType)
            .SetPropertyValue(idProp, idValue);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);

        // Assert
        resp.IsSuccessStatusCode.Should().BeTrue();
        var actual = await resp.Content.ReadFromJsonAsync(resourceType);
        actual.Should().NotBeNull();
        expectedEntity.Should().BeEquivalentTo(actual, o => o.Excluding(x => x.Name == idProp));

        var dbEntity = await client.GetFromJsonAsync($"{baseUrl}/{actual!.GetPropertyValue(idProp)}", resourceType);
        expectedEntity.Should().BeEquivalentTo(dbEntity, o => o.Excluding(x => x.Name == idProp));
    }

    [Theory]
    [InlineData("odata-api/products", typeof(Product), nameof(Product.Category))]
    public async Task Create_ContainsNewComplexProperty_ShouldSuccess(string baseUrl, Type resourceType, string complexPropName)
    {
        // Arrange
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);

        // Act
        var expectedEntity = DataGenerator.Create(resourceType);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var actual = await resp.Content.ReadFromJsonAsync(resourceType);

        var dbEntity = await client.GetFromJsonAsync($"{baseUrl}/{actual!.GetPropertyValue(idProp)}?$expand={complexPropName}", resourceType);

        // Assert
        expectedEntity.Should().BeEquivalentTo(dbEntity);
    }

    [Theory]
    [InlineData("odata-api/products", typeof(Product), nameof(Product.Category), typeof(Category), "odata-api/categories")]
    public async Task Create_ContainsExistingComplexProperty_ShouldSuccess(string baseUrl, Type resourceType
        , string complexPropName, Type complexPropType, string complexPropUrl)
    {
        // Arrange
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);

        //Create complex property
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

    [Theory]
    [InlineData("odata-api/orders", typeof(Order), typeof(Product), nameof(Order.Products), $"{nameof(Order.Products)}($expand=Category)")]
    public async Task Create_ContainsNewCollectionOfComplexProperty_ShouldSuccess(string baseUrl, Type resourceType
        , Type propType
        , string complexPropCollectionName, string customExpand)
    {
        // Arrange
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);

        // Act
        var childCollection = DataGenerator.CreateList(propType, 3);
        var expectedEntity = DataGenerator.Create(resourceType)
            .SetPropertyValue(complexPropCollectionName, childCollection);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);

        // Assert
        resp.IsSuccessStatusCode.Should().BeTrue();

        //compare main entity
        var actual = await resp.Content.ReadFromJsonAsync(resourceType);
        var dbEntity = await client
            .GetFromJsonAsync($"{baseUrl}/{actual!.GetPropertyValue(idProp)}?$expand={customExpand}"
            , resourceType);
        dbEntity.Should().NotBeNull();
        expectedEntity.Should().BeEquivalentTo(dbEntity
            , o => o.Using<decimal>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.01M))
            .WhenTypeIs<decimal>());

    }

    [Theory(Skip = "Need to fix")]
    [InlineData("odata-api/orders", typeof(Order), typeof(Product), "odata-api/products"
        , nameof(Order.Products), $"{nameof(Order.Products)}($expand=Category)")]
    public async Task Create_ContainsExitingCollectionOfComplexProperty_ShouldSuccess(string baseUrl
        , Type resourceType, Type propType, string propUrl
       , string complexPropCollectionName, string customExpand)
    {
        // Arrange
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);

        var childCollection = DataGenerator.CreateList(propType, 3);
        foreach (var child in childCollection)
        {
            var dbChildResp = await client.PostAsJsonAsync(propUrl, child);
            dbChildResp.IsSuccessStatusCode.Should().BeTrue();
        }

        // Act
        var expectedEntity = DataGenerator.Create(resourceType)
            .SetPropertyValue(complexPropCollectionName, childCollection);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);

        // Assert
        resp.IsSuccessStatusCode.Should().BeTrue();

        //compare main entity
        var actual = await resp.Content.ReadFromJsonAsync(resourceType);
        var dbEntity = await client
            .GetFromJsonAsync($"{baseUrl}/{actual!.GetPropertyValue(idProp)}?$expand={customExpand}"
            , resourceType);
        dbEntity.Should().NotBeNull();
        expectedEntity.Should().BeEquivalentTo(dbEntity
            , o => o.Using<decimal>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.01M))
            .WhenTypeIs<decimal>());

    }
}
