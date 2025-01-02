global using CFW.ODataCore.Attributes;

using CFW.EntityMimimalApi.Samples;
using CFW.ODataCore;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SampleDbContext>(
               options => options
               .ReplaceService<IModelCustomizer, ODataModelCustomizer<SampleDbContext>>()
               .EnableSensitiveDataLogging()
               .UseSqlite($@"Data Source=appdbcontext.db"));

builder.Services.AddControllers()
    .AddEntityMinimalApi(o => o.UseDefaultDbContext<SampleDbContext>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseEntityMinimalApi();
//app.MapControllers();

app.Run();
