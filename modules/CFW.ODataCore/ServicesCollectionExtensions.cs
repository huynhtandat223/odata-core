using CFW.ODataCore.Projectors.EFCore;
using CFW.ODataCore.RequestHandlers;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;

namespace CFW.ODataCore;

public static class ServicesCollectionExtensions
{
    public static IMvcBuilder AddODataMinimalApi(this IMvcBuilder mvcBuilder
        , MetadataContainerFactory? metadataContainerFactory = null
        , string defaultRoutePrefix = Constants.DefaultODataRoutePrefix
        , Action<ODataOptions>? odataOptions = null)
    {
        metadataContainerFactory ??= new MetadataContainerFactory();
        var services = mvcBuilder.Services;

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


        return mvcBuilder.AddOData(options =>
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


    }

    public static void UseODataMinimalApi(this WebApplication app)
    {
        var httpRequestHandlers = app.Services.GetServices<IHttpRequestHandler>();
        foreach (var requestHandler in httpRequestHandlers)
        {
            requestHandler.MappRouters(app);
        }
    }
}
