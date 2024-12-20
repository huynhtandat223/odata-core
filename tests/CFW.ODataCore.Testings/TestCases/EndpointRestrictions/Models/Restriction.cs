using CFW.Core.Entities;
using CFW.ODataCore.OData;

namespace CFW.ODataCore.Testings.TestCases.EndpointRestrictions.Models;

[ODataRouting("restrictions", AllowMethods = [ODataMethod.Query, ODataMethod.GetByKey])]
public class Restriction : IEntity<Guid>, IODataViewModel<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[ODataRouting("restrictions-postcreates", AllowMethods = [ODataMethod.PostCreate, ODataMethod.PatchUpdate, ODataMethod.Delete])]
public class RestrictionPostCreate : IEntity<Guid>, IODataViewModel<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

