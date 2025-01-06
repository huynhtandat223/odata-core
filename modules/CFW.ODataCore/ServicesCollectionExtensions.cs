using CFW.ODataCore.Projectors.EFCore;
using CFW.ODataCore.RequestHandlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Parser;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using System.Diagnostics;

namespace CFW.ODataCore;

public static class ServicesCollectionExtensions
{
    public static IServiceCollection AddEntityMinimalApi(this IServiceCollection services
        , Action<EntityMimimalApiOptions>? setupAction = null
        , string defaultRoutePrefix = Constants.DefaultODataRoutePrefix)
    {
        var coreOptions = new EntityMimimalApiOptions();
        if (setupAction is not null)
            setupAction(coreOptions);

        defaultRoutePrefix = SanitizeRoutePrefix(defaultRoutePrefix);

        var containers = coreOptions.MetadataContainerFactory
                .CreateContainers(services, defaultRoutePrefix)
                .ToList();

        services.AddOptions<ODataOptions>().Configure(odataOptions =>
        {
            coreOptions.ODataOptions(odataOptions);
            foreach (var container in containers)
            {
                odataOptions.AddRouteComponents(
                    routePrefix: container.RoutePrefix
                    , model: container.EdmModel);
            }
        });

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

        services.TryAddSingleton<IAssemblyResolver, DefaultAssemblyResolver>();
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
