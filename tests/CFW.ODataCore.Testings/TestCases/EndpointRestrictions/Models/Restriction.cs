using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.TestCases.EndpointRestrictions.Models;

[ODataRouting("restrictions", AllowMethods = [AllowMethod.Query, AllowMethod.GetByKey])]
public class Restriction : IEntity<Guid>, IODataViewModel<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[ODataRouting("restrictions-postcreates", AllowMethods = [AllowMethod.PostCreate, AllowMethod.PatchUpdate, AllowMethod.Delete])]
public class RestrictionPostCreate : IEntity<Guid>, IODataViewModel<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

