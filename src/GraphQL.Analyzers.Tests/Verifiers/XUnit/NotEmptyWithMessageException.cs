using Xunit.Sdk;

namespace GraphQL.Analyzers.Tests.Verifiers.XUnit;

[SuppressMessage("Roslynator", "RCS1194:Implement exception constructors.", Justification = "Copied from NotEmptyException")]
public sealed class NotEmptyWithMessageException : XunitException
{
    private NotEmptyWithMessageException(string message) :
        base(message)
    { }

    public static NotEmptyWithMessageException ForNonEmptyCollection(string userMessage) =>
        new(userMessage + Environment.NewLine + NotEmptyException.ForNonEmptyCollection().Message);
}
