using CFW.Identity;
using CFW.ODataCore;
using CFW.ODataCore.Handlers.Endpoints.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();

builder.Services.AddDbContext<AppDbContext>(
           options => options.UseSqlite("Data Source=appdbcontext.db"));

builder.Services.AddDbContext<ApplicationDbContext>(
           options => options.UseSqlite("Data Source=appidentitycontext.db"));

builder.Services
    .AddGenericODataEndpoints([typeof(Program).Assembly, typeof(EndpointViewModel).Assembly]);

builder.Services.AddCFWIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>();

var app = builder.Build();

app.UseGenericODataEndpoints();
app.UseCFWIdentity<IdentityUser, string>();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.EnsureCreated();

using var scope2 = app.Services.CreateScope();
var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
db2.Database.EnsureCreated();

app.Run();
