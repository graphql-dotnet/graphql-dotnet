using Xunit.Abstractions;
using Xunit.Sdk;

namespace GraphQL.Tests;

/// <summary>
/// This discoverer solves a problem with cluttering of test output with tons of xUnit warnings like that:
/// GraphQL.Tests: Non-serializable data ('System.Object[]') found for 'GraphQL.Tests.Extensions.GraphQLExtensionsTests.ToAST_Test'; falling back to single test case.
/// See https://github.com/xunit/xunit/issues/1473 and https://github.com/xunit/xunit/issues/573.
/// Used by <see cref="TheoryExAttribute"/>.
/// </summary>
public sealed class TheoryExDiscoverer : IXunitTestCaseDiscoverer
{
    private readonly TheoryDiscoverer _discoverer;

    public TheoryExDiscoverer(IMessageSink diagnosticMessageSink)
    {
        _discoverer = new TheoryDiscoverer(new MessageSinkWrapper(diagnosticMessageSink)); // use native discoverer that works with TheoryAttribute
    }

    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        => _discoverer.Discover(discoveryOptions, testMethod, factAttribute);

    private sealed class MessageSinkWrapper : LongLivedMarshalByRefObject, IMessageSink
    {
        private readonly IMessageSink _sink;

        public MessageSinkWrapper(IMessageSink sink)
        {
            _sink = sink;
        }

        public bool OnMessage(IMessageSinkMessage message) =>
            message is IDiagnosticMessage d && d.Message?.StartsWith("Non-serializable data ('System.Object[]') found for") == true
                ? true
                : _sink.OnMessage(message);
    }
}
