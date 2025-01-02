using CFW.ODataCore.Projectors.EFCore;
using CFW.ODataCore.RequestHandlers;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CFW.ODataCore;

public static class ServicesCollectionExtensions
{
    public static IMvcBuilder AddEntityMinimalApi<TDbContext>(this IMvcBuilder mvcBuilder
        , MetadataContainerFactory? metadataContainerFactory = null
        , string defaultRoutePrefix = Constants.DefaultODataRoutePrefix
        , Action<ODataOptions>? odataOptions = null)
        where TDbContext : DbContext
    {
        metadataContainerFactory ??= new MetadataContainerFactory();
        var services = mvcBuilder.Services;

        defaultRoutePrefix = SanitizeRoutePrefix(defaultRoutePrefix);

        var containers = metadataContainerFactory
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
            if (odataOptions is null)
                options.EnableQueryFeatures();
            else
                odataOptions(options);

            foreach (var container in containers)
            {
                options.AddRouteComponents(
                    routePrefix: container.RoutePrefix
                    , model: container.EdmModel);
            }
        });

        services.AddEfCoreProjector<TDbContext>();

        return mvcBuilder;
    }

    public static void UseODataMinimalApi(this WebApplication app)
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
