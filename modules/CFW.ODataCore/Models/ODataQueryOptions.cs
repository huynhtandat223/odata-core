using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Models;

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
            allowedQueryOptions &= ~AllowedQueryOptions.Count;

        if (!queryConfigurations.EnableExpand)
            allowedQueryOptions &= ~AllowedQueryOptions.Expand;

        if (!queryConfigurations.EnableFilter)
            allowedQueryOptions &= ~AllowedQueryOptions.Filter;

        if (!queryConfigurations.EnableOrderBy)
            allowedQueryOptions &= ~AllowedQueryOptions.OrderBy;

        if (!queryConfigurations.EnableSelect)
            allowedQueryOptions &= ~AllowedQueryOptions.Select;

        if (!queryConfigurations.EnableSkipToken)
            allowedQueryOptions &= ~AllowedQueryOptions.SkipToken;

        if (queryConfigurations.MaxTop is not null)
            // Assuming MaxTop being set means Top is allowed, else it's not
            allowedQueryOptions &= ~AllowedQueryOptions.Top;

        IgnoreQueryOptions = ~allowedQueryOptions;
    }
}
