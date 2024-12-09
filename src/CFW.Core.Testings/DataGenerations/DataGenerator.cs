using CFW.Core.Testings.DataGenerations.ObjectGenerators;
using CFW.Core.Utils;
using System.Collections;
using System.Reflection;

namespace CFW.Core.Testings.DataGenerations;

public class DataGenerator
{
    public static T Create<T>()
        => (T)Create(typeof(T))!;

    public static object Create(Type generatingType)
    {
        var dataGenerator = new DataGenerator();
        var result = dataGenerator.Generate(new GeneratorMetadata
        {
            GeneratingType = generatingType,
        })!;
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
        {
            return default;
        }

        var processingType = generatingType;
        if (generatingType.IsGenericType
            && generatingType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            processingType = generatingType.GetGenericArguments()[0];
        }

        var generator = _objectGenerators.FirstOrDefault(x => x.CanGenerate(generatorMetadata));
        if (generator != null)
        {
            return generator.GenerateObject(generatorMetadata);
        }

        var instance = Activator.CreateInstance(processingType);
        var properties = processingType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.CanRead && !p.PropertyType.IsCommonGenericCollectionType())
            .ToList();
        if (properties.Count == 0)
        {
            return instance!;
        }

        foreach (var property in properties)
        {
            var value = Generate(new GeneratorMetadata
            {
                GeneratingType = property.PropertyType,
                PropertyInfo = property,
            });

            if (value != null)
            {
                property.SetValue(instance, value);
            }
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

    public static IList CreateList(Type type, int count = 1)
    {
        var listType = typeof(List<>).MakeGenericType(type);
        var resultList = (IList)Activator.CreateInstance(listType)!;

        for (int i = 0; i < count; i++)
        {
            var instance = Create(type);
            resultList.Add(instance);
        }

        return resultList;
    }
}
