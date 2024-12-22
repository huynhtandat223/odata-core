using CFW.Core.Entities;
using CFW.ODataCore.Attributes;
using CFW.ODataCore.Features.EntitySets;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[ODataEntitySet("multi-authorizations")]
[ODataAuthorize(ApplyMethods = [ODataMethod.Query])]
[ODataAuthorize(ApplyMethods = [ODataMethod.GetByKey], Roles = TestUtils.AdminRole)]
[ODataAuthorize(ApplyMethods = [ODataMethod.PostCreate], Roles = $"{TestUtils.AdminRole},{TestUtils.SupperAdminRole}")]
[ODataAuthorize(ApplyMethods = [ODataMethod.Delete], Roles = TestUtils.SupperAdminRole)]
public class MultiAuthorization : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }
}
