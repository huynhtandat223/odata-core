namespace CFW.CoreTestings.DataGenerations.ObjectGenerators;

public interface IObjectGenerator
{
    public bool CanGenerate(GeneratorMetadata generatorMetadata);

    public object GenerateObject(GeneratorMetadata generatorMetadata);
}
