using CFW.Core.Entities;
using CFW.ODataCore.Models;
using CFW.ODataCore.Testings.Features.Retrictions;
using System.Reflection;

namespace CFW.ODataCore.Testings.TestCases;

public class EndpointRestrictionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EndpointRestrictionTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [InlineData(typeof(Restriction))]
    public async Task SingleModel_MultiEndpoints_CustomMethods_Success(Type dbModelType)
    {
        // Arrange
        var attributes = dbModelType.GetCustomAttributes<EntityAttribute>();

        if (attributes.Count() < 2)
            throw new Exception("Test data invalid. Expect multi endpoints.");

        foreach (var attribute in attributes)
        {
            var baseUrl = $"{attribute!.RoutePrefix ?? Constants.DefaultODataRoutePrefix}/{attribute!.Name}";

            var methodsArray = attribute.Methods;

            if (methodsArray.Length == 0)
                throw new Exception("Test data invalid.No methods are allowed for this endpoint.");

            var client = _factory.CreateClient();
            var dbContext = GetDbContext();

            var data = DataGenerator.CreateList(dbModelType, 5);
            foreach (var item in data)
            {
                dbContext.Add(item);
            }
            dbContext.SaveChanges();
            var keyProp = nameof(IEntity<object>.Id);
            var ids = data.Cast<object>().Select(x => x.GetPropertyValue(keyProp)).ToList();

            foreach (var method in methodsArray)
            {
                if (method == ApiMethod.Query)
                {
                    // Act
                    var odataFilterIds = $"$filter=id in ({string.Join(", ", ids.Select(id => $"'{id}'"))})";
                    var response = await client.GetAsync($"{baseUrl}?{odataFilterIds}");
                    // Assert
                    response.IsSuccessStatusCode.Should().BeTrue();

                    var responseData = response.GetODataQueryResult(dbModelType);
                    responseData.Value.Count().Should().Be(ids.Count);
                }

                if (method == ApiMethod.GetByKey)
                {
                    var expectedEntity = data.Cast<object>().Random();
                    var keyValue = expectedEntity.GetPropertyValue(keyProp);
                    var response = await client.GetAsync($"{baseUrl}/{keyValue}");

                    // Assert
                    var actualEntity = response.GetResponseResult(dbModelType);
                    actualEntity.Should().NotBeNull();
                    actualEntity.Should().BeEquivalentTo(expectedEntity);
                }

                if (method == ApiMethod.Post)
                {
                    var newEntity = DataGenerator.Create(dbModelType);

                    // Act
                    var postResponseMessage = await client.PostAsJsonAsync(baseUrl, newEntity);

                    // Assert
                    postResponseMessage.IsSuccessStatusCode.Should().BeTrue();

                    data = data.Cast<object>().Append(newEntity).ToList();
                }

                if (method == ApiMethod.Patch)
                {
                    continue; // Skip Patch method, not implemented yet.
                    var randomEntity = data.Cast<object>().Random();
                    var keyValue = randomEntity.GetPropertyValue(keyProp);

                    var updatingEntity = DataGenerator.Create(dbModelType)
                        .SetPropertyValue(keyProp, keyValue);

                    // Act
                    var response = await client.PatchAsJsonAsync($"{baseUrl}/{keyValue}", updatingEntity);

                    // Assert
                    response.IsSuccessStatusCode.Should().BeTrue();
                }

                if (method == ApiMethod.Delete)
                {
                    continue; // Skip Patch method, not implemented yet.

                    var randomEntity = data.Cast<object>().Random();
                    var keyValue = randomEntity.GetPropertyValue(keyProp);

                    // Act
                    var deleteResponseMessage = await client.DeleteAsync($"{baseUrl}/{keyValue}");

                    // Assert
                    deleteResponseMessage.IsSuccessStatusCode.Should().BeTrue();
                    data = data.Cast<object>().Where(x => x.GetPropertyValue(keyProp) != keyValue).ToList();
                }
            }
        }

    }

    [Theory]
    [InlineData(typeof(Restriction))]
    public async Task SingleModel_MultiEndpoints_CustomMethods_ExecuteNoAllowMethod_Error(Type dbModelType)
    {
        var attributes = dbModelType.GetCustomAttributes<EntityAttribute>();
        if (attributes.Count() < 2)
            throw new Exception("Test data invalid. Expect multi endpoints.");

        foreach (var attribute in attributes)
        {
            var baseUrl = $"{attribute!.RoutePrefix ?? Constants.DefaultODataRoutePrefix}/{attribute!.Name}";
            var methodsArray = attribute!.Methods;

            if (methodsArray!.Length == 0)
                throw new Exception("Test data invalid. Methods are allowed for this endpoint.");

            var notAllowedMethods = Enum.GetValues<ApiMethod>()
                .Where(x => x != ApiMethod.Get)
                .Except(methodsArray).ToArray();
            if (notAllowedMethods.Length == 0)
                throw new Exception("Test data invalid. No methods are not allowed for this endpoint.");

            var dbContext = GetDbContext();

            var data = DataGenerator.Create(dbModelType);
            dbContext.Add(data);
            var keyValue = data.GetPropertyValue(nameof(IEntity<object>.Id));

            dbContext.SaveChanges();
            var client = _factory.CreateClient();
            foreach (var method in notAllowedMethods)
            {
                if (method == ApiMethod.Query)
                {
                    // Act
                    var queryResponseMessage = await client.GetAsync(baseUrl);

                    // Assert
                    queryResponseMessage.Should().HaveClientError();
                }

                if (method == ApiMethod.GetByKey)
                {
                    var getByKeyResponseMessage = await client.GetAsync($"{baseUrl}/{keyValue}");

                    // Assert
                    getByKeyResponseMessage.Should().HaveClientError();
                }

                if (method == ApiMethod.Post)
                {
                    var newEntity = DataGenerator.Create(dbModelType);
                    // Act
                    var postResponseMessage = await client.PostAsJsonAsync(baseUrl, newEntity);

                    // Assert
                    postResponseMessage.Should().HaveClientError();
                }

                if (method == ApiMethod.Patch)
                {
                    var updatingEntity = DataGenerator.Create(dbModelType)
                        .SetPropertyValue(nameof(IEntity<object>.Id), keyValue);
                    // Act
                    var patchResponseMessage = await client.PatchAsJsonAsync($"{baseUrl}/{keyValue}", updatingEntity);
                    // Assert
                    patchResponseMessage.Should().HaveClientError();
                }

                if (method == ApiMethod.Delete)
                {
                    // Act
                    var deleteResponseMessage = await client.DeleteAsync($"{baseUrl}/{keyValue}");

                    // Assert
                    deleteResponseMessage.Should().HaveClientError();
                }
            }
        }

    }
}
