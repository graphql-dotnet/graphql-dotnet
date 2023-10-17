using Xunit.Sdk;

namespace GraphQL.Analyzers.Tests.Verifiers.XUnit;

[SuppressMessage("Roslynator", "RCS1194:Implement exception constructors.", Justification = "Copied from EmptyException")]
public sealed class EmptyWithMessageException : XunitException
{
    private EmptyWithMessageException(string message) :
        base(message)
    { }

    public static EmptyWithMessageException ForNonEmptyCollection(string collection, string userMessage) =>
        new(userMessage + Environment.NewLine + EmptyException.ForNonEmptyCollection(collection).Message);
}
