using Bogus;

namespace CFW.Core.Testings.DataGenerations.ObjectGenerators;

public class PrimaryTypeGenerator : IObjectGenerator
{
    private static Faker _faker = new Faker();

    private static Dictionary<Func<GeneratorMetadata, bool>, Func<object>> _commonCases
        = new Dictionary<Func<GeneratorMetadata, bool>, Func<object>>()
        {
            { m => m.PropertyInfo is not null
                && m.PropertyInfo.Name.ToLower().Contains("email")
                && m.PropertyInfo.PropertyType == typeof(string), () => _faker.Internet.Email() },
        };

    private static Dictionary<Type, Func<object>> _primaryTypeGenerators = new Dictionary<Type, Func<object>>()
    {
        { typeof(string), () => _faker.Random.Word() },
        { typeof(int), () => _faker.Random.Int(min: 0) },
        { typeof(long), () => _faker.Random.Long() },
        { typeof(decimal), () => _faker.Random.Decimal() },
        { typeof(bool), () => _faker.Random.Bool() },
        { typeof(int?), () => _faker.Random.Int(min: 0) },
        { typeof(long?), () => _faker.Random.Long() },
        { typeof(decimal?), () => _faker.Random.Decimal() },
        { typeof(bool?), () => _faker.Random.Bool() },
        { typeof(DateTime), () => _faker.Date.Past() },
        { typeof(DateTimeOffset), () => _faker.Date.PastOffset() },
        { typeof(DateTime?), () => _faker.Date.Past() },
        { typeof(DateTimeOffset?), () => _faker.Date.PastOffset() },
        { typeof(Guid), () => _faker.Random.Guid() },
        { typeof(Guid?), () => _faker.Random.Guid() }
    };

    public bool CanGenerate(GeneratorMetadata generatorMetadata)
    {
        var commonCase = _commonCases.FirstOrDefault(x => x.Key(generatorMetadata));
        if (commonCase.Key is not null)
            return true;

        return _primaryTypeGenerators.ContainsKey(generatorMetadata.GeneratingType);
    }

    public object GenerateObject(GeneratorMetadata generatorMetadata)
    {
        if (!CanGenerate(generatorMetadata))
            throw new InvalidOperationException();

        var commonCase = _commonCases.FirstOrDefault(x => x.Key(generatorMetadata));
        if (commonCase.Key is not null)
            return commonCase.Value();

        return _primaryTypeGenerators[generatorMetadata.GeneratingType]();

        throw new InvalidOperationException("Can't generate object");
    }
}
