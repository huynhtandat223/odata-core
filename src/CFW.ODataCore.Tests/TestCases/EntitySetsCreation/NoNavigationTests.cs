using CFW.Core.Testings.DataGenerations;
using CFW.Core.Utils;
using CFW.ODataCore.Core;
using CFW.ODataCore.Tests.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CFW.ODataCore.Tests.TestCases.EntitySetsCreation;

public class NoNavigationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NoNavigationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public static IEnumerable<object?[]> Data =>
            new List<object?[]>
            {
                new object?[] { typeof(Category), null }, //Auto generated key
                new object?[] { typeof(Category), Guid.NewGuid() },
                new object?[] { typeof(Customer), 0 }, //Auto generated key
                new object?[] { typeof(Voucher), Guid.NewGuid().ToString() }, //Manual key
            };


    [Theory]
    [MemberData(nameof(Data))]
    public async Task Create_ShouldSuccess(Type resourceType, object? idValue)
    {
        // Arrange
        var baseUrl = resourceType.GetBaseUrl();
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

        expectedEntity.Should()
            .BeEquivalentTo(dbEntity, o => TestUtils.CompareDecimal(o).Excluding(x => x.Name == idProp));
    }

    [Theory]
    [InlineData(typeof(Voucher), null)]
    public async Task Create_ResourceWithNoAutoGenerateKey_ShouldError(Type resourceType, object? defaultId)
    {
        // Arrange
        var baseUrl = resourceType.GetBaseUrl();
        var client = _factory.CreateClient();
        var idProp = nameof(IODataViewModel<object>.Id);

        // Act
        var expectedEntity = DataGenerator.Create(resourceType)
            .SetPropertyValue(idProp, defaultId);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);

        // Assert
        resp.IsSuccessStatusCode.Should().BeFalse();
    }

}
