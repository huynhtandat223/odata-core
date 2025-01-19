
using CFW.ODataCore.Models;
using CFW.ODataCore.Testings.Features.Payments;
using CFW.ODataCore.Testings.Models;
using System.Linq.Expressions;

namespace CFW.ODataCore.Testings.TestCases;

public class EntityConfigurationTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EntityConfigurationTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Payment), typeof(PaymentEndpointConfiguration)])
    {
    }

    [Theory]
    [InlineData(typeof(Payment), typeof(PaymentEndpointConfiguration))]
    public async Task CreateEntity_DefaultConfigure_Success(Type dbModelType
        , Type configurationType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = configurationType.GetAllSupportableMethodBaseUrl();
        var entity = DataGenerator.Create(dbModelType);

        // Act
        var response = await client.PostAsJsonAsync(baseUrl, entity);

        // Assert
        response.Should().BeSuccessful();
        var db = GetDbContext();
        var id = entity.GetPropertyValue(DefaultIdProp);
        var actual = await db.LoadAsync(dbModelType, [id!]);

        var configInstance = Activator.CreateInstance(configurationType);
        var model = configInstance!.GetPropertyValue(nameof(EntityEndpoint<object>.Model)) as LambdaExpression;
        if (model is null)
        {
            actual.Should().BeEquivalentTo(entity, o => o.WithoutStrictOrdering());
            return;
        }

        if (model.Body is not NewExpression newExpression)
        {
            throw new NotImplementedException();
        }

        var allowPropertyNames = newExpression.Members!.Select(x => x.Name).ToArray();
        var excludeProperties = dbModelType.GetProperties()
            .Where(x => !allowPropertyNames.Contains(x.Name))
            .Select(x => x.Name);

        actual.Should().BeEquivalentTo(entity, o => o
            .Excluding(e => excludeProperties.Contains(e.Name))
            .WithoutStrictOrdering());
    }
}
