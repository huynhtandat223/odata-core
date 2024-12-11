using CFW.ODataCore.Tests.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections;
using Xunit.Abstractions;

namespace CFW.ODataCore.Tests.TestCases.EntitySetsQuery;

public class ODataQueryResult<T>
{
    public IEnumerable<T> Value { get; set; } = Array.Empty<T>();
}

public class NoRelationshipQueryTests : BaseTests, IClassFixture<WebApplicationFactory<Program>>
{
    public NoRelationshipQueryTests(ITestOutputHelper testOutputHelper, WebApplicationFactory<Program> factory)
        : base(testOutputHelper, factory)
    {
    }

    [Theory(Skip = "Need to create clearly store this this case")]
    [InlineData(typeof(Category))]
    public async Task Query_ValidData_ShouldSuccess(Type resourceType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = resourceType.GetBaseUrl();

        var entities = DataGenerator.CreateList(resourceType, 10);
        Parallel.For(0, entities.Count, async (i) =>
        {
            var entity = entities[i];
            var resp = await client.PostAsJsonAsync(baseUrl, entity);
            resp.IsSuccessStatusCode.Should().BeTrue();
        });

        // Act
        var responseMessage = await client.GetAsync(baseUrl);
        responseMessage.IsSuccessStatusCode.Should().BeTrue();
        var odataQueryType = typeof(ODataQueryResult<>).MakeGenericType(resourceType);
        var response = await responseMessage.Content.ReadFromJsonAsync(odataQueryType);
        response.Should().NotBeNull();
        var value = response!.GetPropertyValue(nameof(ODataQueryResult<object>.Value)) as IEnumerable;
        value.Should().NotBeNull();
        value!.Cast<object>().Count().Should().Be(10);
    }

    [Theory(Skip = "Need to create clearly store this this case")]
    [InlineData(typeof(Category))]
    public async Task Query_ValidData_Top_ShouldSuccess(Type resourceType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = resourceType.GetBaseUrl();

        var entities = DataGenerator.CreateList(resourceType, 11);
        Parallel.For(0, entities.Count, async (i) =>
        {
            var entity = entities[i];
            var resp = await client.PostAsJsonAsync(baseUrl, entity);
            resp.IsSuccessStatusCode.Should().BeTrue();
        });

        // Act
        var responseMessage = await client.GetAsync(baseUrl + "?$top=10");
        responseMessage.IsSuccessStatusCode.Should().BeTrue();
        var odataQueryType = typeof(ODataQueryResult<>).MakeGenericType(resourceType);
        var response = await responseMessage.Content.ReadFromJsonAsync(odataQueryType);
        response.Should().NotBeNull();
        var value = response!.GetPropertyValue(nameof(ODataQueryResult<object>.Value)) as IEnumerable;
        value.Should().NotBeNull();
        value!.Cast<object>().Count().Should().Be(10);
    }
}
