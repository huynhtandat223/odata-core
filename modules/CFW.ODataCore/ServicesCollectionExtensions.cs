using CFW.ODataCore.Models;
using CFW.ODataCore.Projectors.EFCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Parser;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace CFW.ODataCore;

public class MetadataEntity
{
    public required string Name { get; init; }

    public required Type SourceType { get; init; }

    public required EntityMethod[] Methods { get; init; }

    public required MetadataContainer Container { get; init; }

    public Type? KeyType { get; internal set; }

    public Type? ViewModelType { get; internal set; }
}

public class MetadataContainer
{
    public string RoutePrefix { get; init; }

    public IList<MetadataEntity> MetadataEntities { get; } = new List<MetadataEntity>();

    public EntityMimimalApiOptions Options { get; init; }

    public MetadataContainer(string routePrefix, EntityMimimalApiOptions options)
    {
        RoutePrefix = routePrefix;
        Options = options;
    }

    public IEdmModel EdmModel
    {
        get
        {
            var builder = new ODataConventionModelBuilder();
            foreach (var entity in MetadataEntities)
            {
                var entityType = builder.AddEntityType(entity.SourceType);
                builder.AddEntitySet(entity.Name, entityType);
            }
            return builder.GetEdmModel();
        }
    }
}

public static class ServicesCollectionExtensions
{

    /// <summary>
    /// Don't allow to call this method multiple times yet
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupAction"></param>
    /// <param name="defaultRoutePrefix"></param>
    /// <returns></returns>
    public static IServiceCollection AddEntityMinimalApi(this IServiceCollection services
        , Action<EntityMimimalApiOptions>? setupAction = null
        , string defaultRoutePrefix = Constants.DefaultODataRoutePrefix)
    {
        var coreOptions = new EntityMimimalApiOptions();
        if (setupAction is not null)
            setupAction(coreOptions);

        var sanitizedRoutePrefix = StringUtils.SanitizeRoute(defaultRoutePrefix);
        var containerFactory = coreOptions.MetadataContainerFactory;

        var metadataContainers = containerFactory.CreateMetadataContainers(sanitizedRoutePrefix, coreOptions);
        services.AddSingleton(metadataContainers);

        services.AddSingleton(coreOptions);

        //input, output formatters
        var outputFormaters = ODataOutputFormatterFactory.Create();
        foreach (var formatter in outputFormaters)
        {
            // Fix for issue where JSON formatter does include charset in the Content-Type header
            if (formatter.SupportedMediaTypes.Contains("application/json")
                && !formatter.SupportedMediaTypes.Contains("application/json; charset=utf-8"))
                formatter.SupportedMediaTypes.Add("application/json; charset=utf-8");
        }
        services.AddSingleton<IEnumerable<ODataOutputFormatter>>(outputFormaters);

        var inputFormatters = ODataInputFormatterFactory.Create().Reverse();
        services.AddSingleton(inputFormatters);

        //services.AddODataCore();
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<ODataOptions>, ODataOptionsSetup>());

        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, ODataMvcOptionsSetup>());

        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<JsonOptions>, ODataJsonOptionsSetup>());

        //
        // Parser & Resolver & Provider
        //
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IODataQueryRequestParser, DefaultODataQueryRequestParser>());

        services.TryAddSingleton<IAssemblyResolver>(containerFactory);
        services.TryAddSingleton<IODataPathTemplateParser, DefaultODataPathTemplateParser>();
        //End AddODataCore

        if (coreOptions.DefaultDbContext is not null)
        {
            var contextProvider = typeof(ContextProvider<>).MakeGenericType(coreOptions.DefaultDbContext);
            services.Add(new ServiceDescriptor(typeof(IODataDbContextProvider)
                , contextProvider, coreOptions.DbServiceLifetime));
        }

        return services;
    }

    public static void UseEntityMinimalApi(this WebApplication app)
    {
        var minimalApiOptions = app.Services.GetRequiredService<EntityMimimalApiOptions>();
        var odataOptions = app.Services.GetRequiredService<IOptions<ODataOptions>>().Value;
        var containers = app.Services.GetRequiredService<IEnumerable<MetadataContainer>>();
        using var scope = app.Services.CreateScope();
        var dbContextProvider = scope.ServiceProvider.GetRequiredService<IODataDbContextProvider>();
        var dbContext = dbContextProvider.GetContext();

        foreach (var container in containers)
        {
            if (!container.MetadataEntities.Any())
                continue;

            var containerRoute = app.MapGroup(container.RoutePrefix);
            container.Options.ConfigureContainerRouteGroup?.Invoke(containerRoute);

            foreach (var metadataEntity in container.MetadataEntities)
            {
                if (!metadataEntity.Methods.Any())
                    throw new NotImplementedException($"Handle operations");

                var sourceType = metadataEntity.SourceType;
                var dbEntityType = dbContext.Model.FindEntityType(sourceType);

                if (dbEntityType is null)
                    throw new NotImplementedException($"Only support EF core entities");

                //Find primary key type
                var key = dbEntityType.GetKeys().SingleOrDefault(x => x.IsPrimaryKey());
                if (key is null)
                    throw new InvalidOperationException($"Entity {sourceType.Name} must have primary key");
                metadataEntity.KeyType = key.GetKeyType();
                metadataEntity.ViewModelType = sourceType;

                var entityRoute = containerRoute
                    .MapGroup(metadataEntity.Name)
                    .WithTags(metadataEntity.Name);

                if (metadataEntity.Methods.Contains(EntityMethod.GetByKey) || metadataEntity.Methods.Contains(EntityMethod.Query))
                {
                    var navigations = dbEntityType
                        .GetNavigations();

                    var complextTypes = dbEntityType.GetComplexProperties();
                }
            }

            //odataOptions.AddRouteComponents(
            //    routePrefix: container.RoutePrefix
            //    , model: container.EdmModel);
        }

        //foreach (var container in containers)
        //{
        //    odataOptions.AddRouteComponents(
        //        routePrefix: container.RoutePrefix
        //        , model: container.EdmModel);
        //}

        //var httpRequestHandlers = app.Services.GetServices<IHttpRequestHandler>();
        //foreach (var requestHandler in httpRequestHandlers)
        //{
        //    requestHandler.MappRouters(app);
        //}
    }
}
