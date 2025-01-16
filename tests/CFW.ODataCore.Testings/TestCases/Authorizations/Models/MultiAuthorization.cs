using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[Entity("multi-authorizations")]
[EntityAuthorize(ApplyMethods = [ApiMethod.Query])]
[EntityAuthorize(ApplyMethods = [ApiMethod.GetByKey], Roles = TestUtils.AdminRole)]
[EntityAuthorize(ApplyMethods = [ApiMethod.Post], Roles = $"{TestUtils.AdminRole},{TestUtils.SupperAdminRole}")]
[EntityAuthorize(ApplyMethods = [ApiMethod.Delete], Roles = TestUtils.SupperAdminRole)]
public class MultiAuthorization : IEntity<Guid>
{
    public Guid Id { get; set; }
}
