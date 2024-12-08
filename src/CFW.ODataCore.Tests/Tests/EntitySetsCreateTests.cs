using CFW.Core.Testings.DataGenerations;
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
    [InlineData("odata-api/categories", typeof(Category))]
    //[InlineData("odata-api/customers", typeof(Customer))] //Disable because of the issue can't set the Id (int) = 0 with data generator
    public async Task Create_ShouldSuccess(string baseUrl, Type resourceType)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var expectedEntity = DataGenerator.Create(resourceType);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);

        // Assert
        resp.IsSuccessStatusCode.Should().BeTrue();
        var actual = await resp.Content.ReadFromJsonAsync(resourceType);
        expectedEntity.Should().BeEquivalentTo(actual);
    }
}
