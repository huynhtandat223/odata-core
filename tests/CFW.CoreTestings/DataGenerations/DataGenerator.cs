using CFW.Core.Utils;
using CFW.CoreTestings.DataGenerations.ObjectGenerators;
using System.Collections;
using System.Reflection;

namespace CFW.CoreTestings.DataGenerations;

public class DataGenerator
{
    public static T Create<T>()
        => (T)Create(typeof(T))!;

    public static object Create(Type generatingType, GeneratorMetadata? generatorMetadata = null)
    {
        var dataGenerator = new DataGenerator();
        var metadata = generatorMetadata ?? new GeneratorMetadata();
        metadata.GeneratingType = generatingType;
        var result = dataGenerator.Generate(metadata)!;
        return result;
    }

    private readonly List<IObjectGenerator> _objectGenerators;

    private static CommonGenerator _commonGenerator = new CommonGenerator();
    private static PrimaryTypeGenerator _primaryTypeGenerator = new PrimaryTypeGenerator();

    public DataGenerator()
    {
        _objectGenerators = new List<IObjectGenerator>()
            {
                _commonGenerator,
                _primaryTypeGenerator
            };
    }

    public object? Generate(GeneratorMetadata generatorMetadata)
    {
        var generatingType = generatorMetadata.GeneratingType;

        if (generatingType.IsCommonGenericCollectionType())
            return default;

        var processingType = generatingType;
        if (generatingType.IsGenericType
            && generatingType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            processingType = generatingType.GetGenericArguments()[0];
        }

        var generator = _objectGenerators.FirstOrDefault(x => x.CanGenerate(generatorMetadata));
        if (generator != null)
            return generator.GenerateObject(generatorMetadata);

        var instance = Activator.CreateInstance(processingType);
        var properties = processingType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.CanRead && !p.PropertyType.IsCommonGenericCollectionType())
            .ToList();

        if (generatorMetadata.ExcludeProperties.Any())
            properties = properties.Where(p => !generatorMetadata.ExcludeProperties.Contains(p.Name)).ToList();

        if (properties.Count == 0)
            return instance!;

        foreach (var property in properties)
        {
            var value = Generate(new GeneratorMetadata
            {
                GeneratingType = property.PropertyType,
                PropertyInfo = property,
            });

            if (value != null)
                property.SetValue(instance, value);
        }
        return instance!;
    }

    public static string NewGuidString() => Guid.NewGuid().ToString();

    public static List<T> CreateList<T>(int count = 1)
    {
        var result = new T[count];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = Create<T>();
        }

        return result.ToList();
    }

    public static IList CreateList(Type type, int count = 1, GeneratorMetadata? metadata = null)
    {
        var listType = typeof(List<>).MakeGenericType(type);
        var resultList = (IList)Activator.CreateInstance(listType)!;

        for (int i = 0; i < count; i++)
        {
            var instance = Create(type, metadata);
            resultList.Add(instance);
        }

        return resultList;
    }
}
