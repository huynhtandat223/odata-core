using CFW.Core.Entities;
using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[EndpointEntity("authorize-query-categories")]
[ODataAuthorize(ApplyMethods = [EndpointAction.Query])]
public class AuthorizeQueryCategory : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
