using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[ODataRouting("multi-authorizations")]
[ODataAuthorize(ApplyMethods = [ODataMethod.Query])]
[ODataAuthorize(ApplyMethods = [ODataMethod.GetByKey], Roles = TestUtils.AdminRole)]
[ODataAuthorize(ApplyMethods = [ODataMethod.PostCreate], Roles = $"{TestUtils.AdminRole},{TestUtils.SupperAdminRole}")]
[ODataAuthorize(ApplyMethods = [ODataMethod.Delete], Roles = TestUtils.SupperAdminRole)]
public class MultiAuthorization : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }
}
