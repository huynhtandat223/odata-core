using CFW.Identity;
using CFW.ODataCore;
using CFW.ODataCore.Handlers.Endpoints.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(
           options => options.UseInMemoryDatabase("AppDbContext"));

builder.Services.AddDbContext<ApplicationDbContext>(
           options => options.UseInMemoryDatabase("AppDb"));

builder.Services
    .AddGenericODataEndpoints([typeof(Program).Assembly, typeof(EndpointViewModel).Assembly]);

builder.Services.AddCFWIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>();

var app = builder.Build();

app.UseGenericODataEndpoints();
app.UseCFWIdentity<IdentityUser, string>();

app.Run();
