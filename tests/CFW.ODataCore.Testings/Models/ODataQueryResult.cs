using System.Text.Json.Serialization;

namespace CFW.ODataCore.Testings.Models;

public class ODataQueryResult<TViewModel>
{
    public IEnumerable<TViewModel> Value { set; get; } = Enumerable.Empty<TViewModel>();

    [JsonPropertyName("@odata.count")]
    public int? TotalCount { set; get; }
}
