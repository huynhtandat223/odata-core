﻿using CFW.ODataCore.Models;
using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Attributes;

/// <summary>
/// The class marked with this attribute will be used to create an entity set segment.
/// If entity class: the CRUD operations will be generated base on all.
/// If handler class: the CRUD operations will be generated base on CRUD interfaces.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class EntityAttribute : BaseRoutingAttribute
{
    public Type? TargetType { get; set; }

    public string Name { get; }

    public ApiMethod[]? Methods { get; set; }

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