using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

public partial class InputGraphTypeAnalyzer
{
    public static readonly DiagnosticDescriptor CanNotMatchInputFieldToTheSourceField = new(
        id: DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD,
        title: "Can not match input field to the source field",
        messageFormat: "No property, field or constructor parameter " +
                       "called '{0}' was found on the source type(s) {1}. " +
                       "This field will be ignored during the input deserialization.",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD);

    public static readonly DiagnosticDescriptor CanNotSetSourceField = new(
        id: DiagnosticIds.CAN_NOT_SET_SOURCE_FIELD,
        title: "Can not set source field",
        messageFormat: "The field '{0}' can't be mapped to the {1} '{2}' of the type '{3}' because it's {4}. " +
                       "This field will be ignored during the input deserialization.",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.CAN_NOT_SET_SOURCE_FIELD);

    private static HashSet<string>? AnalyzeInputGraphTypeFields(
        SyntaxNodeAnalysisContext context,
        BaseTypeDeclarationSyntax inputObjectDeclarationSyntax,
        ITypeSymbol sourceTypeSymbol,
        IEnumerable<ISymbol> sourceTypeMembers,
        IMethodSymbol? constructor)
    {
        var allowedSymbols = sourceTypeMembers
            .Concat(constructor?.Parameters ?? Enumerable.Empty<ISymbol>())
            .GroupBy(symbol => symbol.Name, StringComparer.InvariantCultureIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToList(),
                StringComparer.InvariantCultureIgnoreCase);

        HashSet<string>? declaredFiledNames = null;
        foreach (var fieldInvocationExpression in GetDeclaredFields(inputObjectDeclarationSyntax))
        {
            var nameArg = fieldInvocationExpression
                .GetMethodArgument(Constants.ArgumentNames.Name, context.SemanticModel)
                ?.Expression;

            string? fieldName = null;
            switch (nameArg)
            {
                case LiteralExpressionSyntax literal:
                    fieldName = literal.Token.ValueText;
                    break;
                case IdentifierNameSyntax: // ConstField
                case MemberAccessExpressionSyntax: // ConstClass.ConstField
                    var nameSymbol = context.SemanticModel.GetSymbolInfo(nameArg).Symbol;
                    if (nameSymbol is IFieldSymbol { IsConst: true, ConstantValue: string } constSymbol)
                    {
                        fieldName = (string)constSymbol.ConstantValue!;
                    }
                    break;
            }

            var expressionArg = fieldInvocationExpression
                .GetMethodArgument(Constants.ArgumentNames.Expression, context.SemanticModel)
                ?.Expression;

            // don't analyze name for Field("FirstName", source => source.Name), only the accessibility
            if (expressionArg is SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax memberAccessExpression })
            {
                var memberAccessExpressionSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression);
                if (memberAccessExpressionSymbol.Symbol != null)
                {
                    var symbols = new List<ISymbol> { memberAccessExpressionSymbol.Symbol };
                    fieldName ??= memberAccessExpressionSymbol.Symbol.Name;
                    (declaredFiledNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(fieldName);
                    AnalyzeAccessibility(
                        context,
                        nameArg ?? memberAccessExpression,
                        fieldName,
                        symbols);
                }
                continue;
            }

            if (nameArg != null && fieldName != null)
            {
                (declaredFiledNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(fieldName);
                AnalyzeFieldName(context, nameArg, fieldName, allowedSymbols, sourceTypeSymbol);
            }
        }

        return declaredFiledNames;
    }

    private static IEnumerable<InvocationExpressionSyntax> GetDeclaredFields(BaseTypeDeclarationSyntax classDeclaration)
    {
        return classDeclaration
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsFieldInvocation);

        static bool IsFieldInvocation(InvocationExpressionSyntax exp)
        {
            return exp.Expression switch
            {
                // Field
                SimpleNameSyntax nameSyntax =>
                    IsField(nameSyntax),
                // this.Field
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } expressionSyntax =>
                    IsField(expressionSyntax.Name),
                _ => false
            };
        }

        static bool IsField(SimpleNameSyntax simpleNameSyntax) =>
            simpleNameSyntax.Identifier.Text == Constants.MethodNames.Field;
    }

    private static void AnalyzeFieldName(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax nameExpression,
        string fieldName,
        Dictionary<string, List<ISymbol>> allowedSymbols,
        ISymbol sourceTypeSymbol)
    {
        if (allowedSymbols.TryGetValue(fieldName, out var symbols))
        {
            AnalyzeAccessibility(context, nameExpression, fieldName, symbols);
            return;
        }

        string names = sourceTypeSymbol.Name;

        if (sourceTypeSymbol is ITypeParameterSymbol p && p.ConstraintTypes.Any())
        {
            names = string.Join(" or ", p.ConstraintTypes.Select(t => t.Name));
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                CanNotMatchInputFieldToTheSourceField,
                nameExpression.GetLocation(),
                fieldName,
                names));
    }

    private static void AnalyzeAccessibility(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax nameExpression,
        string fieldName,
        List<ISymbol> symbols)
    {
        var diagnostics = symbols.ConvertAll(CheckSymbol);

        // at least one field can be set
        if (diagnostics.Any(diagnostic => diagnostic == null))
        {
            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic!);
        }

        Diagnostic? CheckSymbol(ISymbol symbol)
        {
            List<string>? reasons = null;
            string? symbolType = null;

            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                (reasons ??= []).Add("not 'public'");
            }

            if (symbol.IsStatic)
            {
                (reasons ??= []).Add("'static'");
            }

            switch (symbol)
            {
                case IParameterSymbol:
                    return null;
                case IPropertySymbol property:
                {
                    symbolType = "property";
                    if (property.SetMethod is not { DeclaredAccessibility: Accessibility.Public })
                    {
                        (reasons ??= []).Add("doesn't have a public setter");
                    }
                    break;
                }
                case IFieldSymbol field:
                {
                    symbolType = "field";
                    if (field.IsConst)
                    {
                        _ = reasons!.Remove("'static'");
                        reasons.Add("'const'");
                    }
                    else if (field.IsReadOnly)
                    {
                        (reasons ??= []).Add("'readonly'");
                    }
                    break;
                }
            }

            if (reasons == null)
            {
                return null;
            }

            string reason = reasons.Count == 1
                ? reasons[0]
                : string.Join(", ", reasons.Take(reasons.Count - 1)) + $" and {reasons.Last()}";

            return Diagnostic.Create(
                CanNotSetSourceField,
                nameExpression.GetLocation(),
                fieldName,
                symbolType,
                symbol.Name,
                symbol.ContainingType.Name,
                reason);
        }
    }
}
