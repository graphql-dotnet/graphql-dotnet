using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests.SDK;

public static class SyntaxNodeExtensions
{
    extension(SyntaxNode root)
    {
        /// <summary>
        /// Finds the first Field() invocation in the syntax tree by checking the actual method name
        /// rather than using unreliable string matching.
        /// </summary>
        public InvocationExpressionSyntax FindFieldInvocation() =>
            root.FindMethodInvocation("Field");

        /// <summary>
        /// Finds the first Argument() invocation in the syntax tree.
        /// </summary>
        public InvocationExpressionSyntax FindArgumentInvocation() =>
            root.FindMethodInvocation("Argument");

        /// <summary>
        /// Finds the first invocation of the method with <paramref name="methodName"/> in the syntax tree.
        /// </summary>
        public InvocationExpressionSyntax FindMethodInvocation(string methodName)
        {
            return root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .First(i => i.GetMethodName() == methodName);
        }
    }

    extension(InvocationExpressionSyntax invocation)
    {
        /// <summary>
        /// Extracts the method name from an invocation expression syntax.
        /// </summary>
        public string? GetMethodName()
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                GenericNameSyntax genericName => genericName.Identifier.Text,
                _ => null
            };
        }
    }
}
