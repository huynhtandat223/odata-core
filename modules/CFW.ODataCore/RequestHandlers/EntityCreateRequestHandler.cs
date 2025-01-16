using CFW.ODataCore.Projectors.EFCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.ODataCore.RequestHandlers;

public class JsonDeltaConverter<TSource> : JsonConverter<JsonDelta<TSource>>
{
    private readonly PropertyInfo[] _propertyInfoes;
    private readonly Dictionary<string, Type> _nested;
    private readonly Dictionary<string, Type> _collectionTypes;

    public JsonDeltaConverter(PropertyInfo[] propertyInfos
        , Dictionary<string, Type> nested
        , Dictionary<string, Type> collectionTypes)
    {
        _propertyInfoes = propertyInfos;
        _nested = nested;
        _collectionTypes = collectionTypes;
    }

    public override JsonDelta<TSource> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var delta = new JsonDelta<TSource>();
        delta.ObjectType = typeof(TSource);

        // Create a dictionary to map JSON property names to PropertyInfo
        var propertyMap = _propertyInfoes.ToDictionary(
            p => ResolvePropertyName(p.Name, options),
            p => p,
            StringComparer.OrdinalIgnoreCase // Handle case-insensitivity if needed
        );

        var nestedPropertyMap = _nested.ToDictionary(
            p => ResolvePropertyName(p.Key, options),
            p => p.Value,
            StringComparer.OrdinalIgnoreCase // Handle case-insensitivity if needed
        );

        var collectionPropertyMap = _collectionTypes.ToDictionary(
            p => ResolvePropertyName(p.Key, options),
            p => p.Value,
            StringComparer.OrdinalIgnoreCase // Handle case-insensitivity if needed
        );

        // Parse the JSON
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break; // End of the JSON object
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                // Get the JSON property name
                var jsonPropertyName = reader.GetString();

                // Resolve the matching PropertyInfo
                if (propertyMap.TryGetValue(jsonPropertyName, out var propertyInfo))
                {
                    // Move to the value
                    reader.Read();

                    if (nestedPropertyMap.Keys.Contains(propertyInfo.Name))
                    {
                        var nestedDeltaType = typeof(JsonDelta<>).MakeGenericType(propertyInfo.PropertyType);

                        var nestedDelta = JsonSerializer.Deserialize(ref reader, nestedDeltaType, options);
                        delta.ChangedProperties[propertyInfo.Name] = nestedDelta;
                        continue;
                    }

                    if (collectionPropertyMap.Keys.Contains(propertyInfo.Name))
                    {
                        if (reader.TokenType != JsonTokenType.StartArray)
                            throw new InvalidOperationException("Expected a JSON array.");

                        var elementType = collectionPropertyMap[propertyInfo.Name];

                        var nestedDeltaType = typeof(JsonDelta<>).MakeGenericType(elementType);

                        var deltaArrayType = typeof(JsonDelta<>).MakeGenericType(elementType);
                        var deltaSet = new JsonDeltaSet { ObjectType = elementType };
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            var elementDelta = JsonSerializer
                                .Deserialize(ref reader, deltaArrayType, options) as JsonDelta;
                            deltaSet.ChangedProperties.Add(elementDelta);
                        }

                        delta.ChangedProperties[propertyInfo.Name] = deltaSet;

                        continue;
                    }

                    //Primitive types
                    var propertyValue = JsonSerializer.Deserialize(ref reader, propertyInfo.PropertyType, options);
                    delta.ChangedProperties[propertyInfo.Name] = propertyValue;
                }
                else
                {
                    // Handle unknown properties if needed
                    reader.Skip(); // Skip the value for the unknown property
                }
            }
        }

        return delta;
    }

    public override void Write(Utf8JsonWriter writer, JsonDelta<TSource> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var property in value.ChangedProperties)
        {
            var jsonPropertyName = ResolvePropertyName(property.Key, options);
            writer.WritePropertyName(jsonPropertyName);
            JsonSerializer.Serialize(writer, property.Value, options);
        }
        writer.WriteEndObject();
    }

    private string ResolvePropertyName(string propertyName, JsonSerializerOptions options)
    {
        // Use the naming policy if it's set; otherwise, return the property name as-is
        return options.PropertyNamingPolicy?.ConvertName(propertyName) ?? propertyName;
    }
}

public class JsonDeltaSet
{
    public Type ObjectType { get; set; }

    public List<JsonDelta> ChangedProperties { get; }
        = new List<JsonDelta>();
}

public class JsonDelta
{
    public Type ObjectType { get; set; }

    public Dictionary<string, object?> ChangedProperties { get; }
        = new Dictionary<string, object?>();
}

public class JsonDelta<TDbModel> : JsonDelta
{
}

public interface IEntityCreateRequestHandler
{
    Task MappRoutes(EntityRequestContext entityRequestContext);
}

public class DefaultEntityCreateRequestHandler<TSource> : IEntityCreateRequestHandler
where TSource : class
{
    public Task MappRoutes(EntityRequestContext entityRequestContext)
    {
        var entityMetadata = entityRequestContext.MetadataEntity;

        entityRequestContext.EntityRouteGroupBuider.MapPost("/", async (HttpContext httpContext
            , TSource viewModel
                , CancellationToken cancellationToken) =>
        {
            var db = httpContext.RequestServices.GetRequiredService<IODataDbContextProvider>().GetDbContext();
            db.Add(viewModel);
            await db.SaveChangesAsync();

            return Results.Created("", viewModel);
        }).Produces<TSource>();

        return Task.CompletedTask;
    }


    //public Task MappRouters(WebApplication app)
    //{
    //    var entityGroup = _entityMetadata.Container.CreateOrGetEntityGroup(app, _entityMetadata);
    //    var endpoint = _entityMetadata.EntityEndpoint;

    //    var entityCreationFactory = endpoint.EntityCreateFactory;
    //    entityGroup.MapPost("/", async (HttpContext httpContext
    //    , CancellationToken cancellationToken) =>
    //    {
    //        var rootConverter = new JsonDeltaConverter<TSource>(
    //            _entityMetadata.EntityEndpoint.Properties.Keys.ToArray()
    //                , _entityMetadata.EntityEndpoint.NestedTypes
    //                , _entityMetadata.EntityEndpoint.CollectionTypes);

    //        var jsonSerializerOptions = new JsonSerializerOptions
    //        {
    //            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    //            Converters = { rootConverter }
    //        };

    //        foreach (var x in _entityMetadata.EntityEndpoint.NestedTypes)
    //        {
    //            var nestedConverterType = typeof(JsonDeltaConverter<>).MakeGenericType(x.Value);
    //            var properties = x.Value.GetProperties();

    //            var converter = (JsonConverter)Activator.CreateInstance(nestedConverterType
    //                , args: [properties, new Dictionary<string, Type>(), new Dictionary<string, Type>()]);
    //            jsonSerializerOptions.Converters.Add(converter);
    //        }

    //        foreach (var x in _entityMetadata.EntityEndpoint.CollectionTypes)
    //        {
    //            var nestedConverterType = typeof(JsonDeltaConverter<>).MakeGenericType(x.Value);

    //            //TODO: IMPORTAN: need to refactor now
    //            var properties = x.Value.GetProperties()
    //                .Where(x => !IsCustomType(x.PropertyType) && !IsCollectionOfCustomType(x.PropertyType)).ToArray();

    //            var converter = (JsonConverter)Activator.CreateInstance(nestedConverterType
    //                , args: [properties, new Dictionary<string, Type>(), new Dictionary<string, Type>()]);
    //            jsonSerializerOptions.Converters.Add(converter);
    //        }

    //        var viewModel = JsonSerializer.Deserialize<JsonDelta<TSource>>(httpContext.Request.BodyReader.AsStream()
    //            , jsonSerializerOptions);

    //        var result = await entityCreationFactory(httpContext.RequestServices, viewModel.ChangedProperties);

    //        return Results.Created("", result);

    //    }).Produces<TViewModel>();

    //    return Task.CompletedTask;
    //}

    //[Obsolete("Wrong business logic")]
    //public static bool IsCustomType(Type type)
    //{
    //    // Custom types are typically not part of system namespaces
    //    return type.Namespace.StartsWith("CFW.") && !type.IsEnum;
    //}

    //[Obsolete("Wrong business logic")]
    //public static bool IsCollectionOfCustomType(Type type)
    //{
    //    // Check if the type is a collection
    //    if (typeof(IEnumerable).IsAssignableFrom(type))
    //    {
    //        // For arrays
    //        if (type.IsArray)
    //        {
    //            return IsCustomType(type.GetElementType());
    //        }
    //        // For generic collections
    //        if (type.IsGenericType)
    //        {
    //            var genericArguments = type.GetGenericArguments();
    //            if (genericArguments.Length == 1)
    //            {
    //                return IsCustomType(genericArguments[0]);
    //            }
    //        }
    //    }

    //    return false;
    //}


}

