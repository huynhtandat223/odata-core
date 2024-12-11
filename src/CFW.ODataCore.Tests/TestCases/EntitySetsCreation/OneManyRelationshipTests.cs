using CFW.Core.Testings.DataGenerations;
using CFW.Core.Utils;
using CFW.ODataCore.Core;
using CFW.ODataCore.Tests.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace CFW.ODataCore.Tests.TestCases.EntitySetsCreation;

public class OneManyRelationshipTests : BaseTests, IClassFixture<WebApplicationFactory<Program>>
{
    public OneManyRelationshipTests(ITestOutputHelper testOutputHelper, WebApplicationFactory<Program> factory)
        : base(testOutputHelper, factory)
    {
    }



    [Theory]
    [InlineData(typeof(Order), typeof(Product), nameof(Order.Products), nameof(Product.Category))]
    public async Task Create_ContainsNewCollectionOfComplexProperty_ShouldSuccess(Type resourceType
        , Type propType
        , string complexPropCollectionName, string excludeProp)
    {
        // Arrange
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);
        var baseUrl = resourceType.GetBaseUrl();

        // Act
        var childCollection = DataGenerator.CreateList(propType, 3, new GeneratorMetadata
        {
            ExcludeProperties = [idProp, excludeProp]
        });

        var expectedEntity = DataGenerator.Create(resourceType)
            .SetPropertyValue(complexPropCollectionName, childCollection);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);

        // Assert
        resp.IsSuccessStatusCode.Should().BeTrue();

        //compare main entity
        var actual = await resp.Content.ReadFromJsonAsync(resourceType);
        var dbEntity = await client
            .GetFromJsonAsync($"{baseUrl}/{actual!.GetPropertyValue(idProp)}?$expand={complexPropCollectionName}"
            , resourceType);
        dbEntity.Should().NotBeNull();
        expectedEntity.Should().BeEquivalentTo(dbEntity
            , o => TestUtils.CompareDecimal(o).Excluding(x => x.Name == idProp));
    }
}
