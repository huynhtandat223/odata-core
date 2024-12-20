using Microsoft.AspNetCore.Identity;

namespace CFW.Identity;

public static class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var hasUser = userManager.Users.Any();

        if (!hasUser)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var user = new IdentityUser { UserName = "admin" };
            var result = userManager.CreateAsync(user, "123!@#abcABC").Result;
            if (result.Succeeded)
            {
                var role = new IdentityRole { Name = "Admin" };
                var roleResult = roleManager.CreateAsync(role).Result;
                if (roleResult.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Admin").Wait();
                }
            }
        }
    }
}
