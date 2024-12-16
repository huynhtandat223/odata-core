using CFW.Core.Entities;
using CFW.ODataCore.Core;
using Microsoft.AspNetCore.Identity;

namespace CFW.ODataCore.Handlers.Endpoints.Models;

[ODataRouting("endpoints")]
public class EndpointViewModel : IdentityRole, IEntity<string>, IODataViewModel<string>
{
}
