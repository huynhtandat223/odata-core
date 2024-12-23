using Microsoft.AspNetCore.Authorization;

namespace CFW.ODataCore.Features.Shared;

[Obsolete("Use AuthorizeAttribute instead")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ODataAuthorizeAttribute : AuthorizeAttribute
{
    public ODataMethod[]? ApplyMethods { get; set; }
}

[Obsolete("Use AllowAnonymousAttribute instead")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ODataAllowAnonymousAttribute : AllowAnonymousAttribute
{
    public ODataMethod[]? ApplyMethods { get; set; }
}
