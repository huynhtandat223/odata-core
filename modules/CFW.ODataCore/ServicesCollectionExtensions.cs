using CFW.ODataCore.Attributes;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore;

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

        services.AddOptions<ODataOptions>().Configure(coreOptions.ODataOptions);

        //register metadata containers
        var sanitizedRoutePrefix = StringUtils.SanitizeRoute(defaultRoutePrefix);
        var containerFactory = coreOptions.MetadataContainerFactory;
        var metadataContainers = containerFactory.CreateMetadataContainers(sanitizedRoutePrefix, coreOptions);

        services.AddSingleton(metadataContainers);
        services.AddSingleton(coreOptions);

        //register default db context provider
        if (coreOptions.DefaultDbContext is not null)
        {
            var contextProvider = typeof(ContextProvider<>).MakeGenericType(coreOptions.DefaultDbContext);
            services.Add(new ServiceDescriptor(typeof(IDbContextProvider)
                , contextProvider, coreOptions.DbServiceLifetime));
        }

        //register internal services
        foreach (var metadataContainer in metadataContainers)
        {
            var metadataEntities = metadataContainer.MetadataEntities;
            foreach (var metadataEntity in metadataEntities)
            {
                metadataEntity.AddServices(services);
            }

            var entityOperations = metadataContainer.MetadataEntities.SelectMany(x => x.Operations);
            foreach (var entityOperation in entityOperations)
            {
                entityOperation.AddServices(services);
            }

            var unboundOperations = metadataContainer.UnboundOperations;
            foreach (var unboundOperation in unboundOperations)
            {
                unboundOperation.AddServices(services);
            }
        }
        return services;
    }

    public static void UseEntityMinimalApi(this WebApplication app)
    {
        var minimalApiOptions = app.Services.GetRequiredService<EntityMimimalApiOptions>();
        var odataOptions = app.Services.GetRequiredService<IOptions<ODataOptions>>().Value;
        var containers = app.Services.GetRequiredService<IEnumerable<MetadataContainer>>();
        var defaultModel = new ODataConventionModelBuilder().GetEdmModel();

        foreach (var container in containers)
        {
            if (container.MetadataEntities.Any(x => !x.Methods.Any()))
                throw new NotImplementedException($"Entity no methods, maybe operation, need implement this case");

            var containerGroupRoute = app.MapGroup(container.RoutePrefix);
            container.Options.ConfigureContainerRouteGroup?.Invoke(containerGroupRoute);

            foreach (var entityMetadata in container.MetadataEntities)
            {
                entityMetadata.ODataQueryOptions.SetIgnoreQueryOptions(odataOptions.QueryConfigurations);
                RegisterEntityComponents(app, entityMetadata, containerGroupRoute);
            }

            //register unbound operation routes
            foreach (var operation in container.UnboundOperations)
            {
                var routeMapper = app.Services.GetRequiredKeyedService<IRouteMapper>(operation);
                routeMapper.MapRoutes(containerGroupRoute);
            }

            //Add internal odata service providers
            odataOptions.AddRouteComponents(container.RoutePrefix, defaultModel);
            container.ODataInternalServiceProvider = odataOptions.RouteComponents[container.RoutePrefix].ServiceProvider;
        }
    }

    private static void RegisterEntityComponents(WebApplication app
        , MetadataEntity metadataEntity, RouteGroupBuilder containerGroupRoute)
    {
        var sourceType = metadataEntity.SourceType;
        var allowMethods = metadataEntity.Methods;

        var entityRoute = containerGroupRoute
            .MapGroup(metadataEntity.Name)
        .WithTags(metadataEntity.Name);

        //register entity authorization
        var authorizeAttributes = sourceType.GetCustomAttributes<EntityAuthorizeAttribute>();
        var authorizeDataOfMethods = authorizeAttributes.SelectMany(x => x.ApplyMethods
        .Select(m => new { Method = m, Attribute = x })
        .Where(x => allowMethods.Contains(x.Method))
        .GroupBy(x => x.Method));
        foreach (var authorizeDataOfMethod in authorizeDataOfMethods)
        {
            if (authorizeDataOfMethod.Count() > 1)
                throw new InvalidOperationException($"Duplicate method {authorizeDataOfMethod.Key} found in {sourceType}");

            var authorizeData = authorizeDataOfMethod.Single().Attribute;
            entityRoute = entityRoute.RequireAuthorization([authorizeData]);
        }

        //register CRUD routes
        var entityRouteMappers = app.Services.GetKeyedServices<IRouteMapper>(metadataEntity);
        foreach (var entityRouteMapper in entityRouteMappers)
        {
            entityRouteMapper.MapRoutes(entityRoute);
        }

        //register entity operation routes
        foreach (var operation in metadataEntity.Operations)
        {
            var routeMapper = app.Services.GetRequiredKeyedService<IRouteMapper>(operation);
            routeMapper.MapRoutes(entityRoute);
        }
    }
}
