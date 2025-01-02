global using CFW.ODataCore.Attributes;

using CFW.EntityMimimalApi.Samples;
using CFW.ODataCore;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SampleDbContext>(
               options => options
               .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<SampleDbContext>>()
               .EnableSensitiveDataLogging()
               .UseSqlite($@"Data Source=sample_db.db"));

builder.Services.AddEntityMinimalApi(o => o.UseDefaultDbContext<SampleDbContext>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseEntityMinimalApi();


using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetService<SampleDbContext>();
if (db is not null && !db.Database.CanConnect())
    db.Database.EnsureCreated();

app.Run();
