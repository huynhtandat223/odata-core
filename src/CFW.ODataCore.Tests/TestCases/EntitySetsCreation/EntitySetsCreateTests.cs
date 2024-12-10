using CFW.Core.Testings.DataGenerations;
using CFW.Core.Utils;
using CFW.ODataCore.Core;
using CFW.ODataCore.Tests.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CFW.ODataCore.Tests.TestCases.EntitySetsCreation;

public class EntitySetsCreateTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EntitySetsCreateTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public static IEnumerable<object?[]> ResourceWithOneComplexTypeProp =>
            new List<object?[]>
            {
                new object?[] { typeof(Product), nameof(Product.Category), null },
                new object?[] { typeof(Product), nameof(Product.Category), Guid.NewGuid() }
            };

    [Theory]
    [MemberData(nameof(ResourceWithOneComplexTypeProp))]
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
