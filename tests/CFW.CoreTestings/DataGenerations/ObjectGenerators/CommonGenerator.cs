using Bogus;
using CFW.Core.Utils;

namespace CFW.CoreTestings.DataGenerations.ObjectGenerators;

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
        {
            var random = new Random();
            var enumValues = Enum.GetValues(processingType);
            var randomValue = enumValues.GetValue(random.Next(enumValues.Length));
            return randomValue!;
        }

        throw new NotImplementedException();
    }
}
