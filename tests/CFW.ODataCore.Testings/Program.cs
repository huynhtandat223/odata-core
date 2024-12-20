using CFW.Identity;
using CFW.ODataCore;
using CFW.ODataCore.EFCore;
using CFW.ODataCore.Testings;
using Microsoft.AspNetCore.Identity;
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
app.UseCFWIdentity<IdentityUser, string>();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<TestingDbContext>();
if (!db.Database.CanConnect())
    db.Database.EnsureCreated();

app.Run();
