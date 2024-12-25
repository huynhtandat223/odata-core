using Microsoft.AspNetCore.Authorization;

namespace CFW.ODataCore.Features.Shared;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ODataAuthorizeAttribute : AuthorizeAttribute
{
    public EndpointAction[]? ApplyMethods { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ODataAllowAnonymousAttribute : AllowAnonymousAttribute
{
    public EndpointAction[]? ApplyMethods { get; set; }
}
