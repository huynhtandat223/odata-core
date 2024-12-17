using Bogus;
using CFW.Core.Utils;

namespace CFW.Core.Testings.DataGenerations.ObjectGenerators;

public class CommonGenerator : IObjectGenerator
{
    private static readonly Faker _faker = new Faker();


    public bool CanGenerate(GeneratorMetadata generatorMetadata)
    {
        if (generatorMetadata.GeneratingType.IsEnum
            || generatorMetadata.GeneratingType.IsCommonGenericCollectionType())
        {
            return true;
        }

        return false;
    }

    public object GenerateObject(GeneratorMetadata generatorMetadata)
    {
        var processingType = generatorMetadata.GeneratingType;

        if (processingType.IsEnum)
            return _faker.PickRandom(processingType.GetEnumValues()).GetValue(0)!;

        throw new NotImplementedException();
    }
}
