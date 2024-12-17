using CFW.ODataCore.Handlers.Endpoints.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Handlers.Endpoints;

public class EndpointHandler : IQueryHandler<EndpointViewModel, string>
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public EndpointHandler(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IQueryable> Query(ODataQueryOptions<EndpointViewModel> options, CancellationToken cancellationToken)
    {
        var query = _roleManager.Roles.Select(x => new EndpointViewModel
        {
            Id = x.Id,
            Name = x.Name,
            ConcurrencyStamp = x.ConcurrencyStamp,
            NormalizedName = x.NormalizedName
        });

        var applied = options.ApplyTo(query);

        return await Task.FromResult(applied);
    }
}
