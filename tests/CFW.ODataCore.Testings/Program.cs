using CFW.ODataCore;
using CFW.ODataCore.EFCore;
using CFW.ODataCore.Testings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder();

var id = Guid.NewGuid().ToString();
builder.Services.AddDbContext<TestingDbContext>(
           options => options
           .ReplaceService<IModelCustomizer, ODataModelCustomizer<TestingDbContext>>()
           .EnableSensitiveDataLogging()
           .UseSqlite($@"Data Source=appdbcontext_{id}.db"));

builder.Services
    .AddGenericODataEndpoints()
    .AddAutoPopuplateEntities<TestingDbContext>();


builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<TestingDbContext>();

var app = builder.Build();

app.UseGenericODataEndpoints();

app.MapIdentityApi<IdentityUser>();

app.MapPost("/logout", async (SignInManager<IdentityUser> signInManager,
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

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<TestingDbContext>();
if (!db.Database.CanConnect())
    db.Database.EnsureCreated();

//var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
//var hasUser = userManager.Users.Any();

//if (!hasUser)
//{
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//    var user = new IdentityUser { UserName = "admin" };
//    var result = userManager.CreateAsync(user, "123!@#abcABC").Result;
//    if (result.Succeeded)
//    {
//        var role = new IdentityRole { Name = "Admin" };
//        var roleResult = roleManager.CreateAsync(role).Result;
//        if (roleResult.Succeeded)
//        {
//            userManager.AddToRoleAsync(user, "Admin").Wait();
//        }
//    }
//}

app.Run();
