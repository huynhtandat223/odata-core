using CFW.ODataCore.Testings;
using CFW.ODataCore.Testings.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace CFW.ODataCore.Testings.TestCases.EntitySetsPatch;

public class NoRelationshipPatchTests : BaseTests, IClassFixture<WebApplicationFactory<Program>>
{
    public NoRelationshipPatchTests(ITestOutputHelper testOutputHelper, WebApplicationFactory<Program> factory)
        : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [InlineData(typeof(Category))]
    public async Task Patch_InvalidProperties_ShouldError(Type resourceType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = resourceType.GetBaseUrl();
        var idProp = nameof(IODataViewModel<object>.Id);

        // Act
        var expectedEntity = DataGenerator.Create(resourceType);
        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var seededEntity = await resp.Content.ReadFromJsonAsync(resourceType);
        seededEntity.Should().NotBeNull();

        var id = seededEntity!.GetPropertyValue(idProp);
        var patchEntity = new Dictionary<string, object?>
        {
            { DataGenerator.NewGuidString(), DataGenerator.NewGuidString() }
        };

        var patchResp = await client.PatchAsync($"{baseUrl}/{id}", patchEntity.ToStringContent());
        patchResp.IsSuccessStatusCode.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(Category))]
    public async Task Patch_EntityWithNoRelationship_ShouldSuccess(Type resourceType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = resourceType.GetBaseUrl();
        var idProp = nameof(IODataViewModel<object>.Id);

        // Act
        var expectedEntity = DataGenerator.Create(resourceType);
        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var seededEntity = await resp.Content.ReadFromJsonAsync(resourceType);
        seededEntity.Should().NotBeNull();

        var randomProps = resourceType.GetProperties()
            .Where(x => x.Name != idProp)
            .Random(3);
        var id = seededEntity!.GetPropertyValue(idProp);
        var patchEntity = randomProps.ToDictionary(x => x.Name, x => DataGenerator.Create(x.PropertyType));

        var patchResp = await client.PatchAsync($"{baseUrl}/{id}", patchEntity.ToStringContent());

        // Assert patch props are updated
        patchResp.IsSuccessStatusCode.Should().BeTrue();

        var dbEntity = await client.GetFromJsonAsync($"{baseUrl}/{id}", resourceType);
        dbEntity.Should().NotBeNull();
        var actualDic = dbEntity!.ToDictionary();
        actualDic.Should().Contain(patchEntity!);

        // Assert other props are not updated
        var otherProps = resourceType.GetProperties()
            .Where(x => x.Name != idProp && !randomProps.Contains(x));
        actualDic.Should().Contain(otherProps.ToDictionary(x => x.Name, x => seededEntity!.GetPropertyValue(x.Name)));
    }
}
