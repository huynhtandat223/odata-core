using CFW.Core.Entities;
using CFW.ODataCore.Core.Attributes;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[EndpointEntity("multi-authorizations")]
[ODataAuthorize(ApplyMethods = [EndpointAction.Query])]
[ODataAuthorize(ApplyMethods = [EndpointAction.GetByKey], Roles = TestUtils.AdminRole)]
[ODataAuthorize(ApplyMethods = [EndpointAction.PostCreate], Roles = $"{TestUtils.AdminRole},{TestUtils.SupperAdminRole}")]
[ODataAuthorize(ApplyMethods = [EndpointAction.Delete], Roles = TestUtils.SupperAdminRole)]
public class MultiAuthorization : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }
}
