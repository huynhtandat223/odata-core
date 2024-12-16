using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace CFW.Identity;

/*
How to use:
builder.Services.AddDbContext<ApplicationDbContext>(
           options => options.UseInMemoryDatabase("AppDb"));

builder.Services.AddCFWIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>();

app.UseCFWIdentity<IdentityUser, string>();

 */

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCFWIdentity<TDbContext, TUser, TRole, TKey>(this IServiceCollection services)
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
        where TDbContext : IdentityDbContext<TUser, TRole, TKey>
    {
        services.AddAuthorization();

        //Force all endpoints to require authentication
        //services.AddAuthorization(options =>
        //{
        //    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        //        .RequireAuthenticatedUser()
        //        .Build();
        //});

        services.AddIdentityApiEndpoints<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<TDbContext>();

        return services;
    }

    /*https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-9.0
     * https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-9.0
     The call to MapIdentityApi<TUser> adds the following endpoints to the app:
    POST /register
    POST /login
    POST /refresh
    GET /confirmEmail
    POST /resendConfirmationEmail
    POST /forgotPassword
    POST /resetPassword
    POST /manage/2fa
    GET /manage/info
    POST /manage/info
     */
    public static WebApplication UseCFWIdentity<TUser, TKey>(this WebApplication app)
        where TUser : IdentityUser<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        app.MapIdentityApi<TUser>();

        app.MapPost("/logout", async (SignInManager<TUser> signInManager,
            [FromBody] object empty) =>
        {
            if (empty != null)
            {
                await signInManager.SignOutAsync();
                return Results.Ok();
            }
            return Results.Unauthorized();
        })
        .RequireAuthorization();

        //app.MapGet("/roles", async (RoleManager<IdentityRole> roleManager) =>
        //{
        //    var roles = await roleManager.Roles.ToListAsync();
        //    return Results.Ok(roles);
        //});

        //app.MapPost("/roles", async (RoleManager<IdentityRole> roleManager, string roleName) =>
        //{
        //    var role = await roleManager.CreateAsync(new IdentityRole(roleName));
        //    return Results.Ok(role);
        //});

        SeedData.Initialize(app.Services);

        return app;
    }
}
