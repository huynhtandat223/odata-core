using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.ODataMetadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore.RequestHandlers;


public class EntityRequestHandler<TViewModel, TKey> : IHttpRequestHandler
    where TViewModel : class
{
    private readonly ODataMetadataContainer _container;
    private readonly EntityCRUDRoutingMetadata _metadata;

    public EntityRequestHandler(ODataMetadataContainer container
        , EntityCRUDRoutingMetadata metadata)
    {
        _metadata = metadata;
        _container = container;
    }

    public Task MappRouters(WebApplication webApplication)
    {
        var entityGroup = _container.CreateOrGetEntityGroup(webApplication, _metadata);

        foreach (var method in _metadata.ServiceDescriptors.Keys)
        {
            RouteHandlerBuilder? routeHandlerBuilder = null;
            if (method == EntityMethod.GetByKey)
            {
                routeHandlerBuilder = entityGroup.MapGet("/{key}", async (HttpContext httpContext
                    , [FromServices] IEntityGetByKeyHandler<TViewModel, TKey> handler
                    , TKey key
                    , CancellationToken cancellationToken) =>
                {
                    var feature = AddODataFeature(httpContext);

                    var odataQueryContext = new ODataQueryContext(feature.Model, typeof(TViewModel), feature.Path);
                    var opdataQueryOptions = new ODataQueryOptions<TViewModel>(odataQueryContext, httpContext.Request);

                    var result = await handler.Handle(key, opdataQueryOptions, cancellationToken);
                    if (result.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                        return Results.NotFound();

                    var query = result.Data;
                    return new ODataResults<dynamic> { Data = query };
                }).Produces<TViewModel>();
            }

            if (method == EntityMethod.Patch)
            {
                routeHandlerBuilder = entityGroup.MapPatch("/{key}", async (HttpRequest httpRequest
                    , ODataModel<TViewModel, TKey, Delta<TViewModel>> model
                    , TKey key
                    , [FromServices] IEntityPatchHandler<TViewModel, TKey> handler
                    , CancellationToken cancellationToken) =>
                {
                    var result = await handler.Handle(key, model.Value!, cancellationToken);
                    return new ODataResults<TViewModel> { Data = result.Data };
                }).Produces<TViewModel>();
            }

            if (method == EntityMethod.Post)
            {
                routeHandlerBuilder = entityGroup.MapPost("/", async (TViewModel viewModel
                    , [FromServices] IEntityCreateHandler<TViewModel> handler
                    , CancellationToken cancellationToken) =>
                {
                    var result = await handler.Handle(viewModel!, cancellationToken);
                    if (result.IsSuccess)
                        return Results.Created("", result.Data);

                    return Results.BadRequest(result.Message);
                }).Produces<TViewModel>();
            }

            if (method == EntityMethod.Query)
            {
                routeHandlerBuilder = entityGroup.MapGet("/", async (HttpContext httpContext
                    , [FromServices] IEntityQueryHandler<TViewModel> handler
                    , CancellationToken cancellationToken) =>
                {
                    var feature = AddODataFeature(httpContext);

                    var odataQueryContext = new ODataQueryContext(feature.Model, typeof(TViewModel), feature.Path);
                    var opdataQueryOptions = new ODataQueryOptions<TViewModel>(odataQueryContext, httpContext.Request);

                    var result = await handler.Handle(opdataQueryOptions, cancellationToken);
                    return new ODataResults<IQueryable> { Data = result.Data };
                }).Produces<ODataQueryResult<TViewModel>>();
            }

            if (method == EntityMethod.Delete)
            {
                routeHandlerBuilder = entityGroup.MapDelete("/{key}", async (HttpRequest httpRequest
                    , TKey key
                    , [FromServices] IEntityDeleteHandler<TViewModel, TKey> handler
                    , CancellationToken cancellationToken) =>
                {
                    var result = await handler.Handle(key, cancellationToken);
                    if (result.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                        return Results.NotFound();

                    return Results.Ok();
                }).Produces(200);
            }

            if (routeHandlerBuilder is null)
                throw new InvalidOperationException($"No request handler found for method {method}");

            if (_metadata.AuthorizeDataList.TryGetValue(method, out var authorizeData))
            {
                routeHandlerBuilder = routeHandlerBuilder.RequireAuthorization(authorizeData);
            }
        }

        return Task.CompletedTask;
    }

    public static IODataFeature AddODataFeature(HttpContext httpContext)
    {
        var container = httpContext.GetEndpoint()?.Metadata.OfType<ODataMetadataContainer>().SingleOrDefault();
        if (container is null)
            throw new InvalidOperationException("ODataMetadataContainer not found");

        var odataOptions = httpContext.RequestServices.GetRequiredService<IOptions<ODataOptions>>().Value;

        var entityName = httpContext.GetEndpoint()?.Metadata.OfType<EntityCRUDRoutingMetadata>().SingleOrDefault()?.Name;

        if (entityName.IsNullOrWhiteSpace())
            entityName = httpContext.GetEndpoint()?.Metadata.OfType<EntityOperationMetadata>().SingleOrDefault()?.EntityRoutingName;

        if (entityName is null)
            throw new InvalidOperationException("Entity name not found");

        var edmEntitySet = container.EdmModel.EntityContainer.FindEntitySet(entityName);
        var entitySetSegment = new EntitySetSegment(edmEntitySet);
        var segments = new List<ODataPathSegment> { entitySetSegment };

        if (httpContext.Request.RouteValues.TryGetValue("key", out var key))
        {
            var entityType = edmEntitySet.EntityType();
            var keyName = entityType.DeclaredKey.Single();
            var keySegment = new KeySegment(new Dictionary<string, object> { { keyName.Name, key! } }, entityType
                , edmEntitySet);
            segments.Add(keySegment);
        }

        var path = new ODataPath(segments);
        var feature = new ODataFeature
        {
            Path = path,
            Model = container.EdmModel,
            RoutePrefix = container.RoutePrefix,
        };

        httpContext.Features.Set<IODataFeature>(feature);
        return feature;
    }

}


