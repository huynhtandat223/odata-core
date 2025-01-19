using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Metadata;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.ODataCore.RouteMappers;

public class EntityDeltaConverter<TSource> : JsonConverter<EntityDelta<TSource>>
{
    private readonly MetadataEntity _metadata;

    public EntityDeltaConverter(MetadataEntity metadata)
    {
        _metadata = metadata;
    }
    private string ResolvePropertyName(string propertyName, JsonSerializerOptions options)
    {
        return options.PropertyNamingPolicy?.ConvertName(propertyName) ?? propertyName;
    }
    public override EntityDelta<TSource>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var delta = new EntityDelta<TSource>() { ObjectType = typeof(TSource) };
        delta.Instance = Activator.CreateInstance<TSource>();

        var configuration = Activator.CreateInstance(_metadata.ConfigurationType!) as EntityEndpoint<TSource>;
        var allowProperties = configuration!.GetAllowedProperties();
        var allowComplexProperties = _metadata!.ComplexProperties
            .Where(x => allowProperties.Contains(x.PropertyInfo));

        var allowCollectionProperies = _metadata!.CollectionNavigations
            .Where(x => allowProperties.Contains(x.PropertyInfo));

        var allProperties = _metadata.Properties;

        var propertyMap = allProperties.ToDictionary(
            p => ResolvePropertyName(p.Name, options),
            p => p,
            StringComparer.OrdinalIgnoreCase
        );

        // Parse the JSON
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a JSON object.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var jsonPropertyName = reader.GetString();
                var property = propertyMap[jsonPropertyName!];

                // Move to the value
                reader.Read();

                var propertyInfo = property.PropertyInfo;
                var propertyValue = JsonSerializer.Deserialize(ref reader, propertyInfo!.PropertyType, options);
                propertyInfo.SetValue(delta.Instance, propertyValue);

                if (!allowProperties.Contains(property.PropertyInfo))
                {
                    continue;
                }

                if (property is IComplexProperty)
                {
                    var complexDeltaType = typeof(EntityDelta<>).MakeGenericType(propertyInfo.PropertyType);
                    var nestedDelta = JsonSerializer.Deserialize(ref reader, complexDeltaType, options);
                    delta.ChangedProperties[property.Name] = nestedDelta;
                    continue;
                }

                if (property is INavigation) //colection
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new InvalidOperationException("Expected a JSON array.");

                    var elementType = property.ClrType;

                    var nestedDeltaType = typeof(EntityDelta<>).MakeGenericType(elementType);

                    var deltaArrayType = typeof(EntityDelta<>).MakeGenericType(elementType);
                    var deltaSet = new EntityDeltaSet { ObjectType = elementType };
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        var elementDelta = JsonSerializer
                            .Deserialize(ref reader, deltaArrayType, options) as EntityDelta;
                        deltaSet.ChangedProperties.Add(elementDelta!);
                    }

                    delta.ChangedProperties[propertyInfo.Name] = deltaSet;

                    continue;
                }

                delta.ChangedProperties[property.Name] = propertyValue;
            }
        }

        return delta;
    }

    public override void Write(Utf8JsonWriter writer, EntityDelta<TSource> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class EntityDelta
{
    public required Type ObjectType { get; init; }

    public Dictionary<string, object?> ChangedProperties { get; }
        = new Dictionary<string, object?>();
}

public class EntityDeltaSet
{
    public required Type ObjectType { get; set; }

    public List<EntityDelta> ChangedProperties { get; }
        = new List<EntityDelta>();
}

public class EntityDelta<TEntity> : EntityDelta
{
    public TEntity? Instance { get; set; }
}
