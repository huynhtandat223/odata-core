using CFW.ODataCore.Testings.Features.Customers;
using CFW.ODataCore.Testings.Models;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases;

public class DbEntityCreateCustomHandlerTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public DbEntityCreateCustomHandlerTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Customer), typeof(CustomerCreationHandler)])
    {
    }

    [Theory]
    [InlineData(typeof(Customer))]
    public async Task CreateDbModel_CustomPingPongHandler_Success(Type dbModelType)
    {
        //Arrange
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var setupData = DataGenerator.Create(dbModelType);
        var client = _factory.CreateClient();

        //Act
        var response = await client.PostAsJsonAsync(baseUrl, setupData);

        //Assert
        response.Should().BeSuccessful();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseData = response.GetResponseResult(dbModelType);

        responseData.Should().BeEquivalentTo(setupData);
    }

    [Theory]
    [InlineData(typeof(Customer), new string[] { nameof(Customer.ShippingAddress), nameof(Customer.BillingAddress) })]
    public async Task CustomDbModelCreationHandler_QueryDefaultHandlers_WorkingSuccess(Type dbModelType, string[] complexProps)
    {
        //Arrange
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();

        var setupData = DataGenerator.Create(dbModelType);
        var db = GetDbContext();
        db.Add(setupData);
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();

        //Act
        var response = await client.GetAsync(baseUrl);

        //Assert
        response.Should().BeSuccessful();
        var responseData = response.GetODataQueryResult(dbModelType);
        responseData.Value.Should().BeEquivalentTo([setupData], x => x.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory]
    [InlineData(typeof(Customer), new string[] { nameof(Customer.ShippingAddress), nameof(Customer.BillingAddress) })]
    public async Task CustomDbModelCreationHandler_GetDefaultHandlers_WorkingSuccess(Type dbModelType, string[] complexProps)
    {
        //Arrange
        var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        var client = _factory.CreateClient();

        var setupData = DataGenerator.Create(dbModelType);
        var db = GetDbContext();
        db.Add(setupData);
        await db.SaveChangesAsync();

        var id = setupData.GetPropertyValue(DefaultIdProp);

        //Act
        var response = await client.GetAsync($"{baseUrl}/{id}");

        //Assert
        response.Should().BeSuccessful();
        var responseData = response.GetResponseResult(dbModelType);
        responseData.Should().BeEquivalentTo(setupData, x => x.Excluding(e => complexProps.Contains(e.Name)));
    }
}
