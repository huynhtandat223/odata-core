using CFW.ODataCore.DefaultHandlers;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.RouteMappers;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using System.Linq.Expressions;

namespace CFW.ODataCore.Models.Metadata;

public class MetadataEntity
{
    public required string Name { get; init; }

    public required Type SourceType { get; init; }

    public required ApiMethod[] Methods { get; init; }

    public required MetadataContainer Container { get; init; }

    public required ODataQueryOptions ODataQueryOptions { get; init; }

    public IList<MetadataEntityAction> Operations { get; } = new List<MetadataEntityAction>();

    private static object _lockToken = new();
    private IODataFeature? _cachedFeature;
    public IODataFeature CreateOrGetODataFeature<TSource>()
        where TSource : class
    {
        if (_cachedFeature is not null)
            return _cachedFeature;

        if (SourceType != typeof(TSource))
            throw new InvalidOperationException($"Invalid source type {SourceType} for {typeof(TSource)}");

        lock (_lockToken)
        {
            // Double-check if the feature was created while waiting for the lock.
            if (_cachedFeature is not null)
                return _cachedFeature;

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


    internal IProperty? KeyProperty { get; set; }

    internal Expression<Func<TSource, bool>> BuilderEqualExpression<TSource>(DbSet<TSource> dbSet, object key)
        where TSource : class
    {
        if (SourceType != typeof(TSource))
            throw new InvalidOperationException($"Invalid source type {SourceType} for {typeof(TSource)}");

        //build equal expression
        var parameter = Expression.Parameter(typeof(TSource), "x");
        var propertyExpr = Expression.Property(parameter, KeyProperty!.Name);

        var valueExpr = Expression.Constant(key);
        var equal = Expression.Equal(propertyExpr, valueExpr);
        var predicate = Expression.Lambda<Func<TSource, bool>>(equal, parameter);

        return predicate;
    }

    /// <summary>
    /// Find key property for entity type
    /// </summary>
    /// <param name="dbContext"></param>
    /// <exception cref="InvalidOperationException"></exception>
    internal void InitSourceMetadata(DbContext dbContext)
    {
        if (KeyProperty is not null)
            return;

        var entityType = dbContext.Model.FindEntityType(SourceType);
        if (entityType is null)
            throw new InvalidOperationException($"Entity type {SourceType} not found in DbContext");
        var keyProperty = entityType.FindPrimaryKey();
        if (keyProperty is null)
            throw new InvalidOperationException($"Primary key not found for {SourceType}");

        KeyProperty = keyProperty.Properties.Single();
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-9.0
    /// </summary>
    private static readonly Dictionary<Type, string> _typeToConstraintMap = new()
    {
        { typeof(int), "int" },
        { typeof(bool), "bool" },
        { typeof(DateTime), "datetime" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(Guid), "guid" },
        { typeof(long), "long" },
        { typeof(string), "alpha" } // Example: alpha for alphabetic strings
    };

    internal string GetKeyPattern()
    {
        return _typeToConstraintMap.TryGetValue(KeyProperty!.ClrType, out var constraint)
            ? $"{{key:{constraint}}}"
            : "{key}";
    }

    internal void AddServices(IServiceCollection services)
    {
        foreach (var method in Methods)
        {
            if (method == ApiMethod.Query)
            {
                var getByKeyRouteMapperType = typeof(EntityQueryRouteMapper<>)
                    .MakeGenericType(SourceType);
                services.AddKeyedSingleton(this
                    , (s, k) => (IRouteMapper)ActivatorUtilities.CreateInstance(s, getByKeyRouteMapperType, k));
            }

            if (method == ApiMethod.GetByKey)
            {
                var getByKeyRouteMapperType = typeof(EntityGetByKeyRouteMapper<>)
                    .MakeGenericType(SourceType);
                services.AddKeyedSingleton(this
                    , (s, k) => (IRouteMapper)ActivatorUtilities.CreateInstance(s, getByKeyRouteMapperType, k));
            }

            if (method == ApiMethod.Post)
            {
                services.TryAddScoped(typeof(IEntityCreationHandler<>).MakeGenericType(SourceType)
                    , typeof(EntityCreationHandler<>).MakeGenericType(SourceType));

                var getByKeyRouteMapperType = typeof(EntityCreationRouteMapper<>)
                    .MakeGenericType(SourceType);
                services.AddKeyedSingleton(this
                    , (s, k) => (IRouteMapper)ActivatorUtilities.CreateInstance(s, getByKeyRouteMapperType, k));
            }
        }

    }
}
