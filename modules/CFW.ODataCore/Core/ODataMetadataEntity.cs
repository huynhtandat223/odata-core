using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace CFW.ODataCore.Core;

public class ODataMetadataEntity
{
    public required ODataMetadataContainer Container { get; set; }

    public required TypeInfo ControllerType { get; set; }

    public required string Name { get; set; }

    public required Type ViewModelType { get; set; }

    public AuthorizeAttribute? AuthorizeAttribute { get; set; }

    public AllowAnonymousAttribute? AllowAnonymousAttribute { get; set; }

    public AllowMethod[] AllowMethods { get; set; } = Array.Empty<AllowMethod>();
}
