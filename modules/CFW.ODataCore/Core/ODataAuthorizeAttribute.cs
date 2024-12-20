using Microsoft.AspNetCore.Authorization;

namespace CFW.ODataCore.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ODataAuthorizeAttribute : AuthorizeAttribute
{
    public ODataMethod[]? ApplyMethods { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ODataAllowAnonymousAttribute : AllowAnonymousAttribute
{
    public ODataMethod[]? ApplyMethods { get; set; }
}
