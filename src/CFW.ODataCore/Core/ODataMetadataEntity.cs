using System.Reflection;

namespace CFW.ODataCore.Core;

public class ODataMetadataEntity
{
    public required ODataMetadataContainer Container { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required string Name { get; set; }

    public required Type EntityType { get; set; }
}
