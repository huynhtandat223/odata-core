using CFW.Core.Utils;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CFW.Core.Testings.Models;
public class JsonType<TModel> where TModel : new()
{
    private readonly TModel _model;

    private List<string> _removedProperties = new List<string>();

    private List<string> _includedProperties = new List<string>();

    public JsonType(Expression<Func<TModel>> selector)
    {
        if (selector.Body is not MemberInitExpression memberInitExpression)
            throw new InvalidOperationException("Only support new expression");

        _includedProperties = memberInitExpression.Bindings
            .Select(x => x as MemberAssignment)
            .Select(x => x!.Member.Name).ToList();

        try
        {
            var compiled = selector.Compile();
            var newModel = compiled.Invoke();
            _model = newModel;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Invalid expression", ex);
        }

    }

    public TModel? Model => _model;

    public List<string> RemovedProperties => _removedProperties;

    public List<string> IncludedProperties => _includedProperties;

    private string _jsonString = string.Empty;

    public string AsJson()
    {
        if (_jsonString.IsNotNullOrNotWhiteSpace())
            return _jsonString;

        var json = _model?.ToJsonString();
        if (json == null)
        {
            return string.Empty;
        }

        var jsonNode = JsonNode.Parse(json);
        if (jsonNode is not JsonObject jsonObject)
        {
            throw new InvalidOperationException("The provided JSON is not a valid JSON object.");
        }

        foreach (var property in jsonObject)
        {
            if (!_includedProperties.Contains(property.Key))
            {
                _removedProperties.Add(property.Key);
            }
        }

        foreach (var removedProperty in _removedProperties)
        {
            jsonObject.Remove(removedProperty);
        }

        _jsonString = jsonObject.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        });

        return _jsonString;
    }
}
