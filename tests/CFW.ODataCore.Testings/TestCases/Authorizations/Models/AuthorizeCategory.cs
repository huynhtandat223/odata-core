using CFW.Core.Entities;
using Microsoft.AspNetCore.Authorization;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[ODataRouting("authorize-categories")]
[Authorize]
public class AuthorizeCategory : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
