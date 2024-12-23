using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace CFW.ODataCore.Core;

public interface IMapper
{
    object Map(object source, Type descType);
}

public class JsonMapper : IMapper
{
    private readonly JsonOptions _options;

    public JsonMapper(IOptions<JsonOptions> options)
    {
        _options = options.Value;
    }

    public object Map(object source, Type descType)
    {
        if (source == null) throw new ArgumentNullException();
        return source.JsonConvert(descType, _options.SerializerOptions);
    }
}
