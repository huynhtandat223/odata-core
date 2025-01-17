namespace CFW.ODataCore.Attributes;

public abstract class BaseRoutingAttribute : Attribute
{
    /// <summary>
    /// If empty, the value will be taken from user configuration 
    /// <see cref="ServicesCollectionExtensions.AddEntityMinimalApi(IServiceCollection, Action{EntityMimimalApiOptions}?, string)(IMvcBuilder, CFW.ODataCore.MetadataContainerFactory?, string, Action{Microsoft.AspNetCore.OData.ODataOptions}?)"/>"/>
    /// or default value <see cref="Constants.DefaultODataRoutePrefix"/>.
    /// </summary>
    public string? RoutePrefix { get; set; }
}
