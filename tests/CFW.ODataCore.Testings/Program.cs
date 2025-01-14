using CFW.ODataCore;
using CFW.ODataCore.Projectors.EFCore;
using CFW.ODataCore.Testings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OData.ModelBuilder;
using System.Text.Json;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");

// <see cref="https://github.com/OData/AspNetCoreOData/blob/main/docs/camel_case_scenarios.md>
builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


if (!isTesting)
{
    builder.Services.AddDbContext<TestingDbContext>(
               options => options
               .EnableSensitiveDataLogging()
                .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<TestingDbContext>>()
                .UseSqlite($@"Data Source=appdbcontext.db"));

    //Fix testing mock service
    builder.Services.AddSingleton(new List<object>());
    builder.Services.AddEntityMinimalApi(o => o
        .ConfigureODataModelBuilder(b => b.EnableLowerCamelCase())
        .UseDefaultDbContext<TestingDbContext>());

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

//Authentication
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<TestingDbContext>();

var app = builder.Build();

app.MapIdentityApi<IdentityUser>();
app.UseAuthorization();


if (!isTesting)
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseEntityMinimalApi();

    //odata : Cant fix with default swagger
    //var odataOptions = app.Services.GetRequiredService<IOptions<ODataOptions>>().Value;
    //foreach (var (routePrefix, odataComponent) in odataOptions.RouteComponents)
    //{
    //    var model = odataComponent.EdmModel;
    //    var outputJSON = model.ConvertToOpenApi(new OpenApiConvertSettings
    //    {
    //        PathPrefix = routePrefix,
    //    }).SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

    //    outputJSON = outputJSON.Replace(@"""openapi"": ""3.0.4""", @"""openapi"": ""3.0.3""");

    //    app.MapGet($"/{routePrefix}.json", async context =>
    //    {
    //        context.Response.ContentType = "application/json";
    //        await context.Response.WriteAsync(outputJSON);
    //    });

    //    app.UseSwaggerUI(c =>
    //    {
    //        c.SwaggerEndpoint($"/{routePrefix}.json", "OData API v1");
    //        c.RoutePrefix = string.Empty; // Swagger UI available at root
    //    });
    //}
}
else
{
    app.UseEntityMinimalApi();
}

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