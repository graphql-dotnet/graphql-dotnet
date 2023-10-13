namespace GraphQL.Analyzers.Tests.Verifiers.XUnit;

internal static class ExceptionUtilities
{
    public static Exception Unreachable => new InvalidOperationException("This program location is thought to be unreachable.");
}