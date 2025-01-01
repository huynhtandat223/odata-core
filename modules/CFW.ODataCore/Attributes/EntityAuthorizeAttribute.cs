using CFW.ODataCore.Models;
using Microsoft.AspNetCore.Authorization;

namespace CFW.ODataCore.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EntityAuthorizeAttribute : AuthorizeAttribute
{
    public ODataHttpMethod[] ApplyMethods { get; set; } = Enum.GetValues<ODataHttpMethod>();
}
