using CFW.ODataCore;
using CFW.ODataCore.Testings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    builder.Services.AddDbContext<TestingDbContext>(
               options => options
               .EnableSensitiveDataLogging()
               .UseSqlite($@"Data Source=appdbcontext.db"));

    builder.Services.AddEntityMinimalApi(o => o.UseDefaultDbContext<TestingDbContext>());
}

//Authentication
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<TestingDbContext>();

var app = builder.Build();

app.MapIdentityApi<IdentityUser>();
app.UseAuthorization();


app.UseEntityMinimalApi();


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
var db = scope.ServiceProvider.GetService<TestingDbContext>();
if (db is not null && !db.Database.CanConnect())
    db.Database.EnsureCreated();

app.Run();