using Xunit.Sdk;

namespace GraphQL.Tests;

/// <summary>
/// See comments on <see cref="TheoryExDiscoverer"/>.
/// </summary>
[XunitTestCaseDiscoverer("GraphQL.Tests.TheoryExDiscoverer", "GraphQL.Tests")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TheoryExAttribute : FactAttribute
{
}
