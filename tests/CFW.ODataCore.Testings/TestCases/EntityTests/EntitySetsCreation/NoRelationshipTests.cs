using CFW.Core.Entities;
using CFW.ODataCore.Testings.Models;
using Xunit.Extensions.AssemblyFixture;

namespace CFW.ODataCore.Testings.TestCases.EntityTests.EntitySetsCreation;

public class NoRelationshipTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public NoRelationshipTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {
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
        var idProp = nameof(IEntity<object>.Id);

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
        var idProp = nameof(IEntity<object>.Id);

        // Act
        var expectedEntity = DataGenerator.Create(resourceType)
            .SetPropertyValue(idProp, defaultId);

        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);

        // Assert
        resp.IsSuccessStatusCode.Should().BeFalse();
    }

}
