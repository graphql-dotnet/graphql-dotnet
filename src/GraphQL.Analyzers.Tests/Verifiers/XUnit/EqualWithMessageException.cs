using Xunit.Sdk;

namespace GraphQL.Analyzers.Tests.Verifiers.XUnit;

[SuppressMessage("Roslynator", "RCS1194:Implement exception constructors.", Justification = "Copied from EqualException")]
public sealed class EqualWithMessageException : XunitException
{
    private EqualWithMessageException(string message) :
        base(message)
    { }

    public static EqualWithMessageException ForMismatchedValues(
        object? expected,
        object? actual,
        string userMessage) =>
        new(userMessage + Environment.NewLine + EqualException.ForMismatchedValues(expected, actual).Message);
}
