using CFW.Core.Entities;
using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Testings.TestCases.EndpointRestrictions.Models;

[EndpointEntity("restrictions", AllowMethods = [EndpointAction.Query, EndpointAction.GetByKey])]
public class Restriction : IEntity<Guid>, IODataViewModel<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[EndpointEntity("restrictions-postcreates", AllowMethods = [EndpointAction.PostCreate, EndpointAction.PatchUpdate, EndpointAction.Delete])]
public class RestrictionPostCreate : IEntity<Guid>, IODataViewModel<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

