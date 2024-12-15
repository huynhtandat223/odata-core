using CFW.ODataCore.Core;
using FluentAssertions.Equivalency;
using System.Reflection;
using System.Text;

namespace CFW.ODataCore.Tests;

public static class TestUtils
{
    public static string GetBaseUrl(this Type resourceType)
    {
        var odataRouting = resourceType.GetCustomAttribute<ODataRoutingAttribute>();
        return $"odata-api/{odataRouting!.Name}";
    }

    public static EquivalencyAssertionOptions<TExpectation> CompareDecimal<TExpectation>(
         EquivalencyAssertionOptions<TExpectation> o)
    {
        return o.Using<decimal>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.01M))
            .WhenTypeIs<decimal>();
    }

    public static StringContent ToStringContent(this object content)
    {
        return new StringContent(content.ToJsonString(), Encoding.UTF8, "application/json");
    }

    public static Dictionary<string, object?> ToDictionary(this object obj)
    {
        return obj.GetType().GetProperties()
            .ToDictionary(x => x.Name, x => x.GetValue(obj));
    }
}
