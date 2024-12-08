using System.Reflection;

namespace CFW.Core.Testings.DataGenerations
{
    public class GeneratorMetadata
    {
        public Type GeneratingType { get; set; } = default!;

        public PropertyInfo? PropertyInfo { get; set; }
    }
}
