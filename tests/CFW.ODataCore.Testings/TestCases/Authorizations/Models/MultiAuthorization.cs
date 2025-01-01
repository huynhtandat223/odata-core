using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[Entity("multi-authorizations")]
[EntityAuthorize(ApplyMethods = [ODataHttpMethod.Query])]
[EntityAuthorize(ApplyMethods = [ODataHttpMethod.GetByKey], Roles = TestUtils.AdminRole)]
[EntityAuthorize(ApplyMethods = [ODataHttpMethod.Post], Roles = $"{TestUtils.AdminRole},{TestUtils.SupperAdminRole}")]
[EntityAuthorize(ApplyMethods = [ODataHttpMethod.Delete], Roles = TestUtils.SupperAdminRole)]
public class MultiAuthorization : IEntity<Guid>
{
    public Guid Id { get; set; }
}
