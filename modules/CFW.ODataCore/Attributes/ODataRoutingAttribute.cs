namespace CFW.ODataCore.Attributes;

public abstract class ODataRoutingAttribute : Attribute
{
    /// <summary>
    /// If empty, the value will be taken from user configuration 
    /// <see cref="ServicesCollectionExtensions.AddEntityMinimalApi(IMvcBuilder, CFW.ODataCore.Core.MetadataContainerFactory?, string, Action{Microsoft.AspNetCore.OData.ODataOptions}?)"/>"/>
    /// or default value <see cref="Constants.DefaultODataRoutePrefix"/>.
    /// </summary>
    public string? RoutePrefix { get; set; }
}
