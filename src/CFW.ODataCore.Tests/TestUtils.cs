using CFW.ODataCore.Core;
using FluentAssertions;
using FluentAssertions.Equivalency;
using System.Reflection;

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

}
