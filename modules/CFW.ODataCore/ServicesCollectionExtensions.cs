using CFW.ODataCore.Models;
using CFW.ODataCore.Projectors.EFCore;
using CFW.ODataCore.RequestHandlers;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore;

public record EntityRouteKey(string RoutePrefix, string Name);

public record EntityActionRouteKey(string RoutePrefix, string Name, string ActionName);

public class ODataQueryOptions
{
    public required AllowedQueryOptions? InternalAllowedQueryOptions { get; set; }

    public AllowedQueryOptions IgnoreQueryOptions { get; private set; }

    public void SetIgnoreQueryOptions(DefaultQueryConfigurations queryConfigurations)
    {
        if (InternalAllowedQueryOptions is not null)
        {
            IgnoreQueryOptions = ~InternalAllowedQueryOptions.Value;
            return;
        }

        IgnoreQueryOptions = AllowedQueryOptions.None;

        // Start with "Allow all" bitmask
        var allowedQueryOptions = AllowedQueryOptions.All;

        // Disable specific query options based on global configurations
        if (!queryConfigurations.EnableCount)
        {
            allowedQueryOptions &= ~AllowedQueryOptions.Count;
        }

        if (!queryConfigurations.EnableExpand)
        {
            allowedQueryOptions &= ~AllowedQueryOptions.Expand;
        }

        if (!queryConfigurations.EnableFilter)
        {
            allowedQueryOptions &= ~AllowedQueryOptions.Filter;
        }

        if (!queryConfigurations.EnableOrderBy)
        {
            allowedQueryOptions &= ~AllowedQueryOptions.OrderBy;
        }

        if (!queryConfigurations.EnableSelect)
        {
            allowedQueryOptions &= ~AllowedQueryOptions.Select;
        }

        if (!queryConfigurations.EnableSkipToken)
        {
            allowedQueryOptions &= ~AllowedQueryOptions.SkipToken;
        }

        if (queryConfigurations.MaxTop is not null)
        {
            // Assuming MaxTop being set means Top is allowed, else it's not
            allowedQueryOptions &= ~AllowedQueryOptions.Top;
        }

        IgnoreQueryOptions = ~allowedQueryOptions;
    }
}

public class MetadataEntityAction
{
    public required Type TargetType { get; init; }

    public required string ActionName { get; init; }

    public required Type BoundEntityType { get; init; }

    public required string? EntityName { get; init; }

    public HttpMethod HttpMethod { get; set; } = HttpMethod.Post;

    public required string RoutePrefix { get; init; }

    public required Type ImplementedInterface { get; init; }

    public Type? RequestType { get; private set; }

    public Type? ResponseType { get; private set; }

    public void InitRequestResponseTypes()
    {
        var args = ImplementedInterface.GetGenericArguments();
        if (args.Length > 2)
            throw new InvalidOperationException($"Invalid generic arguments count {args.Length} for {ImplementedInterface.FullName}");

        RequestType = args[1];
        ResponseType = args.Length == 1 ? typeof(Result) : args[2];
    }
}

public class MetadataEntity
{
    public required string Name { get; init; }

    public required Type SourceType { get; init; }

    public required EntityMethod[] Methods { get; init; }

    public required MetadataContainer Container { get; init; }

    public required ODataQueryOptions ODataQueryOptions { get; init; }

    public IList<MetadataEntityAction> Operations { get; } = new List<MetadataEntityAction>();

    private static object _lockToken = new();
    private IODataFeature? _cachedFeature;
    public IODataFeature CreateOrGetODataFeature<TSource>()
        where TSource : class
    {
        if (_cachedFeature is not null)
        {
            return _cachedFeature;
        }

        if (SourceType != typeof(TSource))
        {
            throw new InvalidOperationException($"Invalid source type {SourceType} for {typeof(TSource)}");
        }

        lock (_lockToken)
        {
            // Double-check if the feature was created while waiting for the lock.
            if (_cachedFeature is not null)
            {
                return _cachedFeature;
            }

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TSource>(Name);
            builder.EnableLowerCamelCaseForPropertiesAndEnums();

            var model = builder.GetEdmModel();
            var edmEntitySet = model.EntityContainer.FindEntitySet(Name);
            var entitySetSegment = new EntitySetSegment(edmEntitySet);
            var segments = new List<ODataPathSegment> { entitySetSegment };

            var path = new ODataPath(segments);
            _cachedFeature = new ODataFeature
            {
                Path = path,
                Model = model,
                RoutePrefix = Container.RoutePrefix,
                Services = Container.ODataInternalServiceProvider
            };
        }

        return _cachedFeature;
    }
}

public class MetadataContainer
{
    public string RoutePrefix { get; init; }

    public IList<MetadataEntity> MetadataEntities { get; } = new List<MetadataEntity>();

    public EntityMimimalApiOptions Options { get; init; }

    public IServiceProvider? ODataInternalServiceProvider { get; set; }

    public MetadataContainer(string routePrefix, EntityMimimalApiOptions options)
    {
        RoutePrefix = routePrefix;
        Options = options;
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

        services.AddOptions<ODataOptions>().Configure(coreOptions.ODataOptions);

        var sanitizedRoutePrefix = StringUtils.SanitizeRoute(defaultRoutePrefix);
        var containerFactory = coreOptions.MetadataContainerFactory;

        var metadataContainers = containerFactory.CreateMetadataContainers(sanitizedRoutePrefix, coreOptions);

        services.AddSingleton(metadataContainers);

        services.AddSingleton(coreOptions);

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

            //Add internal odata service providers
            odataOptions.AddRouteComponents(container.RoutePrefix, new ODataConventionModelBuilder().GetEdmModel());
            container.ODataInternalServiceProvider = odataOptions.RouteComponents[container.RoutePrefix].ServiceProvider;
        }
    }

    private static void RegisterEntityComponents(WebApplication app
        , MetadataEntity metadataEntity, RouteGroupBuilder containerGroupRoute)
    {
        var sourceType = metadataEntity.SourceType;

        var entityRoute = containerGroupRoute
            .MapGroup(metadataEntity.Name)
            .WithTags(metadataEntity.Name);

        foreach (var method in metadataEntity.Methods)
        {
            var routeKey = new EntityRouteKey(metadataEntity.Container.RoutePrefix, metadataEntity.Name);
            if (method == EntityMethod.Query)
            {
                var queryRequestHandler = app.Services.GetKeyedService<IEntityQueryRequestHandler>(routeKey);
                if (queryRequestHandler is null)
                {
                    var defaultQueryRequestHandlerType = typeof(DefaultEntityQueryRequestHandler<>).MakeGenericType(sourceType);
                    queryRequestHandler = (IEntityQueryRequestHandler)Activator.CreateInstance(defaultQueryRequestHandlerType)!;
                }

                queryRequestHandler.MappRoutes(new EntityRequestContext
                {
                    MetadataEntity = metadataEntity,
                    App = app,
                    EntityRouteGroupBuider = entityRoute,
                    ContainerRouteGroupBuider = containerGroupRoute
                });
            }
        }

        foreach (var operation in metadataEntity.Operations)
        {
            var routeKey = new EntityActionRouteKey(metadataEntity.Container.RoutePrefix, metadataEntity.Name, operation.ActionName);
            var operationRequestHandler = app.Services.GetKeyedService<IEntityActionRequestHandler>(routeKey);
            if (operationRequestHandler is null)
            {
                operation.InitRequestResponseTypes();

                var defaultOperationRequestHandlerType = operation.ResponseType == typeof(Result)
                    ? typeof(DefaultEntityActionRequestHandler<>).MakeGenericType(operation.RequestType!)
                    : typeof(DefaultEntityActionRequestHandler<,>).MakeGenericType(operation.RequestType!, operation.ResponseType!);

                operationRequestHandler = (IEntityActionRequestHandler)Activator.CreateInstance(defaultOperationRequestHandlerType)!;
            }

            operationRequestHandler.MappRoutes(new EntityActionRequestContext
            {
                MetadataEntity = metadataEntity,
                App = app,
                EntityRouteGroupBuider = entityRoute,
                ContainerRouteGroupBuider = containerGroupRoute,
                EntityActionMetadata = operation
            });
        }
    }
}

public class EntityRequestContext
{
    public required MetadataEntity MetadataEntity { get; init; }

    public required WebApplication App { get; init; }

    public required RouteGroupBuilder EntityRouteGroupBuider { get; init; }

    public required RouteGroupBuilder ContainerRouteGroupBuider { get; init; }
}
