using CFW.ODataCore.Projectors.EFCore;
using CFW.ODataCore.RequestHandlers;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CFW.ODataCore;

public class EntityMimimalApiOptions
{
    internal Type DefaultDbContext { get; set; } = default!;

    internal ServiceLifetime DbServiceLifetime { get; set; } = ServiceLifetime.Scoped;

    public Action<ODataOptions> ODataOptions { get; set; } = (options) => options.EnableQueryFeatures();

    public MetadataContainerFactory MetadataContainerFactory { get; set; } = new MetadataContainerFactory();

    public EntityMimimalApiOptions UseDefaultDbContext<TDbContext>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        DefaultDbContext = typeof(TDbContext);
        DbServiceLifetime = serviceLifetime;

        return this;
    }

    public EntityMimimalApiOptions UseODataOptions(Action<ODataOptions> odataOptions)
    {
        ODataOptions = odataOptions;
        return this;
    }

    public EntityMimimalApiOptions UseMetadataContainerFactory(MetadataContainerFactory metadataContainerFactory)
    {
        MetadataContainerFactory = metadataContainerFactory;
        return this;
    }
}

public static class ServicesCollectionExtensions
{
    public static IMvcBuilder AddEntityMinimalApi(this IMvcBuilder mvcBuilder
        , Action<EntityMimimalApiOptions>? setupAction = null
        , string defaultRoutePrefix = Constants.DefaultODataRoutePrefix)
    {
        var services = mvcBuilder.Services;
        var coreOptions = new EntityMimimalApiOptions();
        if (setupAction is not null)
            setupAction(coreOptions);

        defaultRoutePrefix = SanitizeRoutePrefix(defaultRoutePrefix);

        var containers = coreOptions.MetadataContainerFactory
            .CreateContainers(services, defaultRoutePrefix)
            .ToList();

        services.AddSingleton<IMapper, JsonMapper>();

        var formatters = ODataOutputFormatterFactory.Create();
        foreach (var formatter in formatters)
        {
            // Fix for issue where JSON formatter does include charset in the Content-Type header
            if (formatter.SupportedMediaTypes.Contains("application/json")
                && !formatter.SupportedMediaTypes.Contains("application/json; charset=utf-8"))
                formatter.SupportedMediaTypes.Add("application/json; charset=utf-8");
        }
        services.AddSingleton<IEnumerable<ODataOutputFormatter>>(formatters);

        mvcBuilder.AddOData(options =>
        {
            coreOptions.ODataOptions(options);

            foreach (var container in containers)
            {
                options.AddRouteComponents(
                    routePrefix: container.RoutePrefix
                    , model: container.EdmModel);
            }
        });

        if (coreOptions.DefaultDbContext is not null)
        {
            var contextProvider = typeof(ContextProvider<>).MakeGenericType(coreOptions.DefaultDbContext);
            services.Add(new ServiceDescriptor(typeof(IODataDbContextProvider)
                , contextProvider, coreOptions.DbServiceLifetime));
        }
        return mvcBuilder;
    }

    public static void UseEntityMinimalApi(this WebApplication app)
    {
        var httpRequestHandlers = app.Services.GetServices<IHttpRequestHandler>();
        foreach (var requestHandler in httpRequestHandlers)
        {
            requestHandler.MappRouters(app);
        }
    }

    /// <summary>
    /// From Microsoft.AspNetCore.OData source code
    /// Sanitizes the route prefix by stripping leading and trailing forward slashes.
    /// </summary>
    /// <param name="routePrefix">Route prefix to sanitize.</param>
    /// <returns>Sanitized route prefix.</returns>
    private static string SanitizeRoutePrefix(string routePrefix)
    {
        Debug.Assert(routePrefix != null);

        if (routePrefix.Length > 0 && routePrefix[0] != '/' && routePrefix[^1] != '/')
        {
            return routePrefix;
        }

        return routePrefix.Trim('/');
    }
}
