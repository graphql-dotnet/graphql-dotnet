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

    private static void AnalyzeInputGraphTypeFields(
        SyntaxNodeAnalysisContext context,
        BaseTypeDeclarationSyntax inputObjectDeclarationSyntax,
        ITypeSymbol sourceTypeSymbol)
    {
        var graphQlConstructorAttribute = context.Compilation
            .GetTypeByMetadataName(Constants.MetadataNames.GraphQLConstructorAttribute);

        var allowedSymbols = (sourceTypeSymbol is ITypeParameterSymbol parameterSymbol
                ? GetAllowedFieldNames(parameterSymbol.ConstraintTypes, graphQlConstructorAttribute)
                : GetAllowedFieldNames(sourceTypeSymbol, graphQlConstructorAttribute))
            .GroupBy(symbol => symbol.Name, StringComparer.InvariantCultureIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToList(),
                StringComparer.InvariantCultureIgnoreCase);

        foreach (var fieldInvocationExpression in GetDeclaredFields(inputObjectDeclarationSyntax))
        {
            // don't analyze fields marked with NoClrMapping
            var noClrMapping = (fieldInvocationExpression.Parent as ExpressionSyntax)
                ?.FindMethodInvocationExpression(Constants.MethodNames.NoClrMapping);

            if (noClrMapping != null)
                continue;

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
            if (expressionArg is SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax mem })
            {
                var expressionSymbol = context.SemanticModel.GetSymbolInfo(mem);
                if (expressionSymbol.Symbol != null)
                {
                    var symbols = new List<ISymbol> { expressionSymbol.Symbol };
                    AnalyzeAccessibility(
                        context,
                        nameArg ?? mem,
                        fieldName ?? expressionSymbol.Symbol.Name,
                        symbols);
                }
                continue;
            }

            if (nameArg != null && fieldName != null)
            {
                AnalyzeFieldName(context, nameArg, fieldName, allowedSymbols, sourceTypeSymbol);
            }
        }
    }

    private static IEnumerable<ISymbol> GetAllowedFieldNames(
        IEnumerable<ITypeSymbol> sourceTypeSymbols,
        INamedTypeSymbol? graphQlConstructorAttribute) =>
        sourceTypeSymbols.SelectMany(sourceTypeSymbol =>
            GetAllowedFieldNames(sourceTypeSymbol, graphQlConstructorAttribute));

    private static IEnumerable<ISymbol> GetAllowedFieldNames(
        ITypeSymbol sourceTypeSymbol,
        INamedTypeSymbol? graphQlConstructorAttribute)
    {
        // consider ctor params on the type itself but not base classes
        var symbols = FindConstructor(sourceTypeSymbol, graphQlConstructorAttribute)
            ?.Parameters ?? Enumerable.Empty<ISymbol>();

        var nullableSourceTypeSymbol = sourceTypeSymbol;
        while (nullableSourceTypeSymbol != null)
        {
            var fieldsOrProperties = nullableSourceTypeSymbol
                .GetMembers()
                .Where(symbol => !symbol.IsImplicitlyDeclared && symbol is IPropertySymbol or IFieldSymbol);

            symbols = symbols.Concat(fieldsOrProperties);
            nullableSourceTypeSymbol = nullableSourceTypeSymbol.BaseType;
        }

        return symbols;
    }

    // Mimic the AutoRegisteringHelper.GetConstructorOrDefault behavior
    private static IMethodSymbol? FindConstructor(
        ITypeSymbol sourceTypeSymbol,
        ISymbol? graphQlConstructorAttribute)
    {
        var constructors = sourceTypeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => method is
            {
                MethodKind: MethodKind.Constructor,
                DeclaredAccessibility: Accessibility.Public,
                IsImplicitlyDeclared: false
            })
            .ToList();

        // if there are no public constructors, return null
        if (constructors.Count == 0)
        {
            return null;
        }

        // if there is only one public constructor, return it
        if (constructors.Count == 1)
        {
            return constructors[0];
        }

        // if there are multiple public constructors, return the one marked with
        // GraphQLConstructorAttribute, or the parameterless constructor, or null
        IMethodSymbol? match = null;
        IMethodSymbol? parameterless = null;
        foreach (var constructor in constructors)
        {
            if (constructor.GetAttributes().Any(attr =>
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, graphQlConstructorAttribute)))
            {
                // when multiple constructors decorated with [GraphQLConstructor]
                // we ignore all the constructors in this analyzer
                if (match != null)
                {
                    return null;
                }
                match = constructor;
            }

            if (constructor.Parameters.Length == 0)
            {
                parameterless = constructor;
            }
        }

        return match ?? parameterless;
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
        IDictionary<string, List<ISymbol>> allowedSymbols,
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
