using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullableReferenceTypeArgumentAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor NullableReferenceTypeShouldSpecifyNullable = new(
        id: DiagnosticIds.NULLABLE_REFERENCE_TYPE_ARGUMENT_SHOULD_SPECIFY_NULLABLE,
        title: "Nullable reference type argument should specify nullable parameter",
        messageFormat: "Nullable reference type '{0}' should explicitly specify nullable: true",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.NULLABLE_REFERENCE_TYPE_ARGUMENT_SHOULD_SPECIFY_NULLABLE);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(NullableReferenceTypeShouldSpecifyNullable);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
    {
        var invocationExpr = (InvocationExpressionSyntax)context.Node;

        // Check if this is a member access (e.g., .Argument(...))
        if (invocationExpr.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        // Check if the method name is "Argument"
        if (memberAccess.Name.Identifier.Text != Constants.MethodNames.Argument)
            return;

        // Check if this is a GraphQL method
        if (!memberAccess.IsGraphQLSymbol(context.SemanticModel))
            return;

        // Get the method symbol
        if (context.SemanticModel.GetSymbolInfo(invocationExpr, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
            return;

        // Check if this is a generic method with one type argument
        if (!methodSymbol.IsGenericMethod || methodSymbol.TypeArguments.Length != 1)
            return;

        var typeArgument = methodSymbol.TypeArguments[0];

        // Check if the type argument is a nullable reference type
        if (!IsNullableReferenceType(typeArgument))
            return;

        // Check if nullable parameter is explicitly provided
        var nullableArg = invocationExpr.GetMethodArgument(Constants.ArgumentNames.Nullable, context.SemanticModel);

        if (nullableArg == null)
        {
            // No nullable argument provided - report diagnostic
            var typeArgumentSyntax = ((GenericNameSyntax)memberAccess.Name).TypeArgumentList.Arguments[0];
            var diagnostic = Diagnostic.Create(
                NullableReferenceTypeShouldSpecifyNullable,
                typeArgumentSyntax.GetLocation(),
                typeArgument.ToDisplayString());
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            // Check if the argument is null or default literal (not a variable or expression)
            if (IsNullOrDefaultLiteral(nullableArg.Expression))
            {
                var typeArgumentSyntax = ((GenericNameSyntax)memberAccess.Name).TypeArgumentList.Arguments[0];
                var diagnostic = Diagnostic.Create(
                    NullableReferenceTypeShouldSpecifyNullable,
                    typeArgumentSyntax.GetLocation(),
                    typeArgument.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
            // If it's a variable or other expression, don't report (we can't determine the value at compile time)
        }
    }

    private static bool IsNullableReferenceType(ITypeSymbol typeSymbol)
    {
        // Check if it's a reference type with nullable annotation
        return typeSymbol.IsReferenceType &&
               typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }

    private static bool IsNullOrDefaultLiteral(ExpressionSyntax expression)
    {
        return expression.IsKind(SyntaxKind.NullLiteralExpression) ||
               expression.IsKind(SyntaxKind.DefaultLiteralExpression) ||
               (expression is LiteralExpressionSyntax literal &&
                literal.Token.IsKind(SyntaxKind.NullKeyword));
    }
}
