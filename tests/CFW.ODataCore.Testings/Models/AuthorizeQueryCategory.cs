using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.Models;

[Entity("authorize-query-categories")]
[EntityAuthorize(ApplyMethods = [ApiMethod.Query])]
public class AuthorizeQueryCategory : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
