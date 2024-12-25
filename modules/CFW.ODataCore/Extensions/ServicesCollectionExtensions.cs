using CFW.ODataCore.Core.MetadataResolvers;
using CFW.ODataCore.Features.EFCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Extensions;

public static class ServicesCollectionExtensions
{
    public static IServiceCollection AddGenericDbContext<TDbContext>(this IServiceCollection services
        , ServiceLifetime dbServiceLifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        var contextProvider = typeof(ContextProvider<>).MakeGenericType(typeof(TDbContext));
        services.Add(new ServiceDescriptor(typeof(IODataDbContextProvider), contextProvider, dbServiceLifetime));

        return services;
    }

    public static IMvcBuilder AddGenericODataEndpoints(this IMvcBuilder mvcBuilder
        , BaseODataMetadataResolver? oDataTypeResolver = null, Action<ODataOptions>? odataOptions = null)
    {
        oDataTypeResolver ??= new DefaultODataMetadataResolver(Constants.DefaultODataRoutePrefix);
        var containers = oDataTypeResolver.CreateContainers();

        var services = mvcBuilder.Services;

        foreach (var container in containers)
        {
            foreach (var metadata in container.EntitySetMetadataList)
            {
                services.AddScoped(metadata.ServiceHandlerType, metadata.ServiceImplemenationType);

                var serviceType = typeof(ODataRequestHandler);
                var requestHandlerType = typeof(ODataRequestHandler<>).MakeGenericType(metadata.ViewModelType);
                var keyName = metadata.Container.RoutePrefix + metadata.RoutingAttribute.Name;

                services.AddKeyedScoped(serviceType, keyName, (s, key) =>
                {
                    return ActivatorUtilities.CreateInstance(s, requestHandlerType, metadata.Container);
                });
            }

            foreach (var metadata in container.BoundOperationMetadataList)
            {
                services.AddScoped(metadata.ServiceHandlerType, metadata.ServiceImplemenationType);
            }

            foreach (var metadata in container.UnBoundOperationMetadataList)
            {
                services.AddScoped(metadata.ServiceHandlerType, metadata.ServiceImplemenationType);
            }
        }

        services.AddSingleton(new GenericODataConfig { IsEnabled = true });
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        services.AddSingleton<IMapper, JsonMapper>();

        mvcBuilder = Build(containers, mvcBuilder, odataOptions);
        return mvcBuilder;
    }

    public static IMvcBuilder Build(IEnumerable<ODataMetadataContainer> containers
        , IMvcBuilder mvcBuilder, Action<ODataOptions>? odataOptions)
    {
        mvcBuilder = mvcBuilder.AddMvcOptions(options =>
        {
            foreach (var container in containers)
            {
                options.Conventions.Add(new EntityAPIRoutingConvention(container));
            }
        });

        return mvcBuilder.AddOData(options =>
        {
            if (odataOptions is null)
                options.EnableQueryFeatures();
            else
                odataOptions(options);

            foreach (var metadataContainer in containers)
            {
                options.AddRouteComponents(
                    routePrefix: metadataContainer.RoutePrefix
                    , model: metadataContainer.EdmModel);
            }
        }).ConfigureApplicationPartManager(pm =>
        {
            foreach (var metadataContainer in containers)
            {
                pm.ApplicationParts.Add(metadataContainer);
            }
        });
    }

    public static void UseGenericODataEndpoints(this WebApplication app)
    {
        var odataConfig = app.Services.GetService<GenericODataConfig>();
        if (odataConfig is null || !odataConfig.IsEnabled)
            return;

        app.MapGet("{routePrefix}/{entitySetName}", async (HttpRequest httpRequest
            , [FromServices] IServiceProvider serviceProvider
            , string routePrefix
            , string entitySetName) =>
        {
            var key = routePrefix + entitySetName;
            var requestHandler = serviceProvider.GetRequiredKeyedService<ODataRequestHandler>(key);
            await requestHandler.Execute(httpRequest, routePrefix, entitySetName);
        });


        app.UseRouting();
        app.MapControllers();
    }
}
