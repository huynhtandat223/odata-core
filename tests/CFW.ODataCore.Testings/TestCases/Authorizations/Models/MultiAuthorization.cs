using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[Entity("multi-authorizations")]
[EntityAuthorize(ApplyMethods = [EntityMethod.Query])]
[EntityAuthorize(ApplyMethods = [EntityMethod.GetByKey], Roles = TestUtils.AdminRole)]
[EntityAuthorize(ApplyMethods = [EntityMethod.Post], Roles = $"{TestUtils.AdminRole},{TestUtils.SupperAdminRole}")]
[EntityAuthorize(ApplyMethods = [EntityMethod.Delete], Roles = TestUtils.SupperAdminRole)]
public class MultiAuthorization : IEntity<Guid>
{
    public Guid Id { get; set; }
}
