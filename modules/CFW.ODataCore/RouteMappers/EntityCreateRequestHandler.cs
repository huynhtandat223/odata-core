using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.ODataCore.RouteMappers;

[Obsolete("End investigation")]
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
            throw new JsonException("Expected a JSON object.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break; // End of the JSON object

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

[Obsolete("End investigation")]
public class JsonDeltaSet
{
    public Type ObjectType { get; set; }

    public List<JsonDelta> ChangedProperties { get; }
        = new List<JsonDelta>();
}

[Obsolete("End investigation")]
public class JsonDelta
{
    public Type ObjectType { get; set; }

    public Dictionary<string, object?> ChangedProperties { get; }
        = new Dictionary<string, object?>();
}

[Obsolete("End investigation")]
public class JsonDelta<TDbModel> : JsonDelta
{
}
