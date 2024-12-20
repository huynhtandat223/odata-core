using CFW.ODataCore.EFCore;
using CFW.ODataCore.Extensions;
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

app.Run();
