using CFW.Core.Entities;
using CFW.ODataCore.Attributes;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[ODataEntitySet("authorize-query-categories")]
[ODataAuthorize(ApplyMethods = [ODataMethod.Query])]
public class AuthorizeQueryCategory : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
