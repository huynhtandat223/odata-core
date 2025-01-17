using CFW.ODataCore.Models;
using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Attributes;


[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class EntityAttribute : BaseRoutingAttribute
{
    internal Type? TargetType { get; set; }

    public string Name { get; }

    public ApiMethod[] Methods { get; set; } = [ApiMethod.GetByKey, ApiMethod.Post, ApiMethod.Put, ApiMethod.Patch, ApiMethod.Delete, ApiMethod.Query];

    /// <summary>
    /// OData query options for method <see cref="ApiMethod.Query"/> or <see cref="ApiMethod.GetByKey"/>
    /// </summary>
    public AllowedQueryOptions? AllowedQueryOptions { get; set; }

    public EntityAttribute(string name)
    {
        if (name.IsNullOrWhiteSpace())
        {
            throw new ArgumentNullException(nameof(name));
        }

        Name = name;
    }
}