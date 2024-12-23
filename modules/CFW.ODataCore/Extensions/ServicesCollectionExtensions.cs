using CFW.ODataCore.Core.MetadataResolvers;
using CFW.ODataCore.Features.BoundOperations;
using CFW.ODataCore.Features.EFCore;
using CFW.ODataCore.Features.EntityCreate;
using CFW.ODataCore.Features.EntitySets.Handlers;
using CFW.ODataCore.Features.UnBoundOperations;
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
        services.AddScoped(typeof(IRequestHandler<,>), typeof(DefaultRequestHandler<,>));
        services.AddScoped(typeof(IQueryHandler<,>), typeof(DefaultQueryHandler<,>));
        services.AddScoped(typeof(IGetByKeyHandler<,>), typeof(DefaultGetByKeyHandler<,>));
        services.AddScoped(typeof(IDeleteHandler<,>), typeof(DefaultDeleteHandler<,>));
        services.AddScoped(typeof(IPatchHandler<,>), typeof(DefaultPatchHandler<,>));

        services.AddScoped(typeof(IBoundOperationRequestHandler<,,,>), typeof(DefaultBoundOperationRequestHandler<,,,>));
        services.AddScoped(typeof(IUnboundOperationRequestHandler<,>), typeof(DefaultUnboundOperationRequestHandler<,>));

        foreach (var container in containers)
        {
            var metadataList = container.APIMetadataList;
            foreach (var metadata in metadataList)
            {
                if (metadata is BoundAPIMetadata boundAPIMetadata)
                {
                    var viewModelType = boundAPIMetadata.ViewModelType;
                    var keyType = boundAPIMetadata.KeyType;
                    var serviceType = typeof(IEntityCreateHandler<,>).MakeGenericType(viewModelType, keyType);
                    var implementationType = typeof(DefaultEntityCreateHandler<,>).MakeGenericType(viewModelType, keyType);

                    services.AddScoped(serviceType, s => ActivatorUtilities.CreateInstance(s, implementationType, metadata));
                    continue;
                }
                throw new NotImplementedException();
                //TODO: need use keyed service

            }

            //var boundOperationMetadata = container.EntityMetadataList
            //    .SelectMany(x => x.BoundOperationMetadataList)
            //    .ToList();
            //foreach (var metadata in boundOperationMetadata)
            //{
            //    var serviceType = metadata.ResponseType == typeof(Result)
            //        ? typeof(IODataOperationHandler<>).MakeGenericType(metadata.RequestType)
            //        : typeof(IODataOperationHandler<,>).MakeGenericType(metadata.RequestType, metadata.ResponseType);

            //    services.AddScoped(serviceType, metadata.HandlerType);
            //}

            //foreach (var metadata in container.UnBoundOperationMetadataList)
            //{
            //    var serviceType = metadata.ResponseType == typeof(Result)
            //        ? typeof(IODataOperationHandler<>).MakeGenericType(metadata.RequestType)
            //        : typeof(IODataOperationHandler<,>).MakeGenericType(metadata.RequestType, metadata.ResponseType);

            //    services.AddScoped(serviceType, metadata.HandlerType);
            //}
        }

        services.AddSingleton(new GenericODataConfig { IsEnabled = true });
        services.AddScoped<IActionContextAccessor, ActionContextAccessor>();
        services.AddSingleton<IMapper, JsonMapper>();

        return Build(containers, mvcBuilder, odataOptions);
    }

    public static IMvcBuilder Build(IEnumerable<ODataMetadataContainer> containers
        , IMvcBuilder mvcBuilder, Action<ODataOptions>? odataOptions)
    {
        var entitySetControllerTypes = containers
            .SelectMany(x => x.EntityMetadataList.Select(x => x.ControllerType))
            .ToList();
        mvcBuilder = mvcBuilder.AddMvcOptions(options =>
        {
            foreach (var container in containers)
            {
                options.Conventions.Add(new EntityCreateConvention(container));

                //options.Conventions.Add(new EntitySetsConvention(container));

                //var hasBoundOperations = container.EntityMetadataList
                //    .SelectMany(x => x.BoundOperationMetadataList)
                //    .Any();
                //if (hasBoundOperations)
                //    options.Conventions.Add(new BoundOperationsConvention(container));

                //if (container.UnBoundOperationMetadataList.Any())
                //    options.Conventions.Add(new UnboundOperationsConvention(container));
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

        app.UseRouting();
        app.MapControllers();
    }
}
