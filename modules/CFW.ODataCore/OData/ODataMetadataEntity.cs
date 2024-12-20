using System.Reflection;

namespace CFW.ODataCore.OData;

public class ODataMetadataEntity
{
    public required ODataMetadataContainer Container { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required string Name { get; set; }

    public required Type ViewModelType { get; set; }

    public Attribute[] SetupAttributes { get; set; } = Array.Empty<Attribute>();
}
