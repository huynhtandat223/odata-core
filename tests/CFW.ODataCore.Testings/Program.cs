using CFW.Identity;
using CFW.ODataCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();

var id = Guid.NewGuid().ToString();
builder.Services.AddDbContext<AppDbContext>(
           options => options.UseSqlite($@"Data Source=appdbcontext_{id}.db"));

builder.Services.AddDbContext<ApplicationDbContext>(
           options => options.UseSqlite($@"Data Source=appidentitycontext{id}.db"));

builder.Services
    .AddGenericODataEndpoints();

builder.Services.AddCFWIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>();

var app = builder.Build();

app.UseGenericODataEndpoints();
app.UseCFWIdentity<IdentityUser, string>();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
if (!db.Database.CanConnect())
    db.Database.EnsureCreated();

using var scope2 = app.Services.CreateScope();
var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
if (!db2.Database.CanConnect())
    db2.Database.EnsureCreated();

app.Run();
