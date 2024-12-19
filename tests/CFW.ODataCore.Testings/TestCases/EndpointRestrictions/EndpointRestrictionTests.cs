using CFW.Core.Entities;
using CFW.ODataCore.Testings.TestCases.EndpointRestrictions.Models;
using CFW.ODataCore.Testings.TestCases.EntitySetsQuery;
using System.Collections;
using System.Net;
using System.Reflection;

namespace CFW.ODataCore.Testings.TestCases.EndpointRestrictions;

public class EndpointRestrictionTests : BaseTests, IClassFixture<AppFactory>
{
    public EndpointRestrictionTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [InlineData(typeof(Restriction))]
    [InlineData(typeof(RestrictionPostCreate))]
    public async Task Request_WithCustomAlowMethods_ShouldSuccess(Type odataViewModelType)
    {
        // Arrange
        var baseUrl = odataViewModelType.GetBaseUrl();
        var routingAttribute = odataViewModelType.GetCustomAttribute<ODataRoutingAttribute>();
        var methodsArray = routingAttribute!.AllowMethods;

        if (methodsArray!.Length == 0)
        {
            throw new Exception("Test data invalid.No methods are allowed for this endpoint.");
        }

        var client = _factory.CreateClient();
        var dbContext = _factory.Services.GetRequiredService<AppDbContext>();

        var data = DataGenerator.CreateList(odataViewModelType, 5);
        foreach (var item in data)
        {
            dbContext.Add(item);
        }
        dbContext.SaveChanges();
        var keyProp = nameof(IEntity<object>.Id);


        foreach (var method in methodsArray)
        {
            if (method == AllowMethod.Query)
            {
                // Act
                var queryResponseMessage = await client.GetAsync(baseUrl);
                // Assert
                queryResponseMessage.IsSuccessStatusCode.Should().BeTrue();

                var odataQueryType = typeof(ODataQueryResult<>).MakeGenericType(odataViewModelType);
                var response = await queryResponseMessage.Content.ReadFromJsonAsync(odataQueryType);
                response.Should().NotBeNull();
                var value = response!.GetPropertyValue(nameof(ODataQueryResult<object>.Value)) as IEnumerable;
                value.Should().NotBeNull();
                value!.Cast<object>().Count().Should().Be(data.Count);
            }

            if (method == AllowMethod.GetByKey)
            {
                var expectedEntity = data.Cast<object>().Random();
                var keyValue = expectedEntity.GetPropertyValue(keyProp);
                var getByKeyResponseMessage = await client.GetAsync($"{baseUrl}/{keyValue}");

                // Assert
                getByKeyResponseMessage.IsSuccessStatusCode.Should().BeTrue();
                var actualEntity = await getByKeyResponseMessage.Content.ReadFromJsonAsync(odataViewModelType);
                actualEntity.Should().NotBeNull();

                actualEntity.Should().BeEquivalentTo(expectedEntity);
            }

            if (method == AllowMethod.PostCreate)
            {
                var newEntity = DataGenerator.Create(odataViewModelType);

                // Act
                var postResponseMessage = await client.PostAsJsonAsync(baseUrl, newEntity);

                // Assert
                postResponseMessage.IsSuccessStatusCode.Should().BeTrue();

                data = data.Cast<object>().Append(newEntity).ToList();
            }

            if (method == AllowMethod.PatchUpdate)
            {
                var randomEntity = data.Cast<object>().Random();
                var keyValue = randomEntity.GetPropertyValue(keyProp);

                var updatingEntity = DataGenerator.Create(odataViewModelType)
                    .SetPropertyValue(keyProp, keyValue);

                // Act
                var patchResponseMessage = await client.PatchAsJsonAsync($"{baseUrl}/{keyValue}", updatingEntity);

                // Assert
                patchResponseMessage.IsSuccessStatusCode.Should().BeTrue();

            }

            if (method == AllowMethod.Delete)
            {
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

    [Theory]
    [InlineData(typeof(Restriction))]
    [InlineData(typeof(RestrictionPostCreate))]
    public async Task Request_WithoutCustomAlowMethods_ShouldMethodNotAllow(Type odataViewModelType)
    {
        var baseUrl = odataViewModelType.GetBaseUrl();
        var routingAttribute = odataViewModelType.GetCustomAttribute<ODataRoutingAttribute>();
        var methodsArray = routingAttribute!.AllowMethods;

        if (methodsArray!.Length == 0)
        {
            throw new Exception("Test data invalid. Methods are allowed for this endpoint.");
        }

        var notAllowedMethods = Enum.GetValues<AllowMethod>().Except(methodsArray).ToArray();
        if (notAllowedMethods.Length == 0)
        {
            throw new Exception("Test data invalid. No methods are not allowed for this endpoint.");
        }

        var dbContext = _factory.Services.GetRequiredService<AppDbContext>();

        var data = DataGenerator.Create(odataViewModelType);
        dbContext.Add(data);
        var keyValue = data.GetPropertyValue(nameof(IEntity<object>.Id));

        dbContext.SaveChanges();
        var client = _factory.CreateClient();
        foreach (var method in notAllowedMethods)
        {
            if (method == AllowMethod.Query)
            {
                // Act
                var queryResponseMessage = await client.GetAsync(baseUrl);
                // Assert
                queryResponseMessage.IsSuccessStatusCode.Should().BeFalse();
                queryResponseMessage.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
            }

            if (method == AllowMethod.GetByKey)
            {
                var getByKeyResponseMessage = await client.GetAsync($"{baseUrl}/{keyValue}");
                // Assert
                getByKeyResponseMessage.IsSuccessStatusCode.Should().BeFalse();
                getByKeyResponseMessage.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
            }

            if (method == AllowMethod.PostCreate)
            {
                var newEntity = DataGenerator.Create(odataViewModelType);
                // Act
                var postResponseMessage = await client.PostAsJsonAsync(baseUrl, newEntity);
                // Assert
                postResponseMessage.IsSuccessStatusCode.Should().BeFalse();
                postResponseMessage.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
            }

            if (method == AllowMethod.PatchUpdate)
            {
                var updatingEntity = DataGenerator.Create(odataViewModelType)
                    .SetPropertyValue(nameof(IEntity<object>.Id), keyValue);
                // Act
                var patchResponseMessage = await client.PatchAsJsonAsync($"{baseUrl}/{keyValue}", updatingEntity);
                // Assert
                patchResponseMessage.IsSuccessStatusCode.Should().BeFalse();
                patchResponseMessage.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
            }

            if (method == AllowMethod.Delete)
            {
                // Act
                var deleteResponseMessage = await client.DeleteAsync($"{baseUrl}/{keyValue}");
                // Assert
                deleteResponseMessage.IsSuccessStatusCode.Should().BeFalse();
                deleteResponseMessage.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
            }
        }
    }
}
