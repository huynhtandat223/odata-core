using CFW.Core.Builders;
using CFW.ODataCore.Models;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace CFW.ODataCore.RequestHandlers;

public class EntityMetadata<TEntity, TViewModel, TKey> : EntityMetadata
{
    public EntityMetadata(ODataMetadataContainer container
        , IOptions<ODataOptions> odataOptions
        , EntityConfiguration<TEntity> entityConfiguration)
        : base(container)
    {
        EndpointName = entityConfiguration.Name;
        EntityEndpoint = entityConfiguration;

        var queryOptions = new QueryOptions();
        IgnoreQueryOptions = AllowedQueryOptions.None;
        if (EntityEndpoint.QueryOptionConfig is not null)
        {
            EntityEndpoint.QueryOptionConfig(queryOptions);
            IgnoreQueryOptions = ~queryOptions.AllowedQueryOptions;
        }
        else
        {
            var globalOptions = odataOptions.Value;

            // Start with "Allow all" bitmask
            var allowedQueryOptions = AllowedQueryOptions.All;

            // Disable specific query options based on global configurations
            if (!globalOptions.QueryConfigurations.EnableCount)
            {
                allowedQueryOptions &= ~AllowedQueryOptions.Count;
            }

            if (!globalOptions.QueryConfigurations.EnableExpand)
            {
                allowedQueryOptions &= ~AllowedQueryOptions.Expand;
            }

            if (!globalOptions.QueryConfigurations.EnableFilter)
            {
                allowedQueryOptions &= ~AllowedQueryOptions.Filter;
            }

            if (!globalOptions.QueryConfigurations.EnableOrderBy)
            {
                allowedQueryOptions &= ~AllowedQueryOptions.OrderBy;
            }

            if (!globalOptions.QueryConfigurations.EnableSelect)
            {
                allowedQueryOptions &= ~AllowedQueryOptions.Select;
            }

            if (!globalOptions.QueryConfigurations.EnableSkipToken)
            {
                allowedQueryOptions &= ~AllowedQueryOptions.SkipToken;
            }

            if (globalOptions.QueryConfigurations.MaxTop is not null)
            {
                // Assuming MaxTop being set means Top is allowed, else it's not
                allowedQueryOptions &= ~AllowedQueryOptions.Top;
            }

            IgnoreQueryOptions = ~allowedQueryOptions;
        }

        ViewModelSelector = ExpressionTreeBuilder
            .BuildMappingExpression<TEntity, TViewModel>();

        if (ViewModelSelector is null)
            throw new InvalidOperationException("Can't build view model selector");
    }


    internal EntityConfiguration<TEntity> EntityEndpoint { get; }

    internal Expression<Func<TEntity, TViewModel>> ViewModelSelector { get; }

    /// <summary>
    /// (query, key) => query.Key.Equals(key)
    /// </summary>
    public Expression<Func<TViewModel, bool>> GetByKeyExpression(TKey key)
    {
        var parameter = Expression.Parameter(typeof(TViewModel), "x");
        var property = Expression.Property(parameter, EntityEndpoint.KeyPropertyName);
        var value = Expression.Constant(key);
        var equal = Expression.Equal(property, value);
        var predicate = Expression.Lambda<Func<TViewModel, bool>>(equal, parameter);

        return predicate;
    }
}
