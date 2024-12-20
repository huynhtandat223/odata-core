using CFW.Core.Entities;
using CFW.ODataCore.OData;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[ODataRouting("authorize-categories")]
[ODataAuthorize]
public class AuthorizeCategory : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}


[ODataRouting("authorize-query-categories")]
[ODataAuthorize(ApplyMethods = [ODataMethod.Query])]
public class AuthorizeQueryCategory : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
