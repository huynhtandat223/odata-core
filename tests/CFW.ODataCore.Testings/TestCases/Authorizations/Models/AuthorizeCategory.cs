using CFW.Core.Entities;
using CFW.ODataCore.Core.Attributes;
using Microsoft.AspNetCore.Authorization;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[EndpointEntity("authorize-categories")]
[Authorize]
public class AuthorizeCategory : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
