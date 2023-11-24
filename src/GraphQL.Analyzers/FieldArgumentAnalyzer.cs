using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FieldArgumentAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor DoNotUseObsoleteArgumentMethod = new(
        id: DiagnosticIds.DO_NOT_USE_OBSOLETE_ARGUMENT_METHOD,
        title: "Don't use an obsolete 'Argument' method",
        messageFormat: "Don't use an obsolete 'Argument' method",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.DO_NOT_USE_OBSOLETE_ARGUMENT_METHOD);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DoNotUseObsoleteArgumentMethod);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
    }

    private void AnalyzeMemberAccessExpression(SyntaxNodeAnalysisContext context)
    {
        var argumentMemberAccessExpression = (MemberAccessExpressionSyntax)context.Node;
        if (argumentMemberAccessExpression.Name.Identifier.Text != Constants.MethodNames.Argument)
        {
            return;
        }

        if (!argumentMemberAccessExpression.IsGraphQLSymbol(context.SemanticModel))
        {
            return;
        }

        if (argumentMemberAccessExpression.Name is not GenericNameSyntax genericName)
        {
            return;
        }

        // assuming there is no other Argument<,>() methods with 2 type parameters
        if (genericName.TypeArgumentList.Arguments.Count != 2)
        {
            return;
        }

        var x = context.SemanticModel.GetSymbolInfo(genericName);
        var xx = (IMethodSymbol)x.Symbol!.OriginalDefinition;
        var attr = xx.TypeArguments[1].GetAttributes();

        ;
        var diagnostic = Diagnostic.Create(
            DoNotUseObsoleteArgumentMethod,
            argumentMemberAccessExpression.GetMethodInvocationLocation());

        context.ReportDiagnostic(diagnostic);
    }
}
