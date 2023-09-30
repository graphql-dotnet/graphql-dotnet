using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InputGraphTypeAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor InvalidInputField = new(
        id: DiagnosticIds.INVALID_INPUT_FIELD,
        title: "Invalid input field",
        messageFormat: "No instance property with public setter, public field or public constructor parameter " +
                       "called '{0}' was found on the source type(s) {1}. " +
                       "This field will be ignored during the input deserialization.",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.INVALID_INPUT_FIELD);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(InvalidInputField);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var sourceTypeSymbol = GetSourceTypeSymbol(classDeclaration, context);
        if (sourceTypeSymbol == null || sourceTypeSymbol.SpecialType == SpecialType.System_Object)
        {
            return;
        }

        var allowedFieldNames = (sourceTypeSymbol is ITypeParameterSymbol parameterSymbol
                ? GetAllowedFieldNames(parameterSymbol.ConstraintTypes)
                : GetAllowedFieldNames(sourceTypeSymbol))
            .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        foreach (var fieldInvocationExpression in GetDeclaredFields(classDeclaration))
        {
            var expressionArg = fieldInvocationExpression
                .GetMethodArgument(Constants.ArgumentNames.Expression, context.SemanticModel)
                ?.Expression;

            // don't analyze Field("FirstName", source => source.Name)
            if (expressionArg != null)
            {
                return;
            }

            var nameArg = fieldInvocationExpression
                .GetMethodArgument(Constants.ArgumentNames.Name, context.SemanticModel)
                ?.Expression;

            switch (nameArg)
            {
                case LiteralExpressionSyntax literal:
                    if (!allowedFieldNames.Contains(literal.Token.ValueText))
                    {
                        ReportDiagnostic(nameArg, literal.Token.ValueText);
                    }

                    break;
                case IdentifierNameSyntax: // ConstField
                case MemberAccessExpressionSyntax: // ConstClass.ConstField
                    var nameSymbol = context.SemanticModel.GetSymbolInfo(nameArg).Symbol;
                    if (nameSymbol is IFieldSymbol { IsConst: true } fieldSymbol)
                    {
                        ReportDiagnostic(nameArg, (string)fieldSymbol.ConstantValue!);
                    }

                    break;
            }
        }

        void ReportDiagnostic(ExpressionSyntax nameExpression, string fieldName)
        {
            if (allowedFieldNames.Contains(fieldName))
            {
                return;
            }

            string names = sourceTypeSymbol.Name;

            if (sourceTypeSymbol is ITypeParameterSymbol p && p.ConstraintTypes.Any())
            {
                names = string.Join(" or ", p.ConstraintTypes.Select(t => t.Name));
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    InvalidInputField,
                    nameExpression.GetLocation(),
                    fieldName,
                    names));
        }
    }

    private static ITypeSymbol? GetSourceTypeSymbol(
        ClassDeclarationSyntax inputClassDeclaration,
        SyntaxNodeAnalysisContext context)
    {
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(inputClassDeclaration);
        if (typeSymbol == null)
        {
            return null;
        }

        // quick test for interface implementation before iterating on base types
        if (!typeSymbol.AllInterfaces.Any(i => i.Name == Constants.Interfaces.IInputObjectGraphType))
        {
            return null;
        }

        var genericInputObjectGraphType = context.Compilation.GetTypeByMetadataName("GraphQL.Types.InputObjectGraphType`1");
        if (genericInputObjectGraphType == null)
        {
            return null;
        }

        var sourceTypeSymbol = typeSymbol;
        while (sourceTypeSymbol != null)
        {
            if (SymbolEqualityComparer.Default.Equals(sourceTypeSymbol.OriginalDefinition, genericInputObjectGraphType))
            {
                return sourceTypeSymbol.TypeArguments.Single(); // <TSourceType>
            }

            sourceTypeSymbol = sourceTypeSymbol.BaseType;
        }

        return null;
    }

    private static IEnumerable<string> GetAllowedFieldNames(IEnumerable<ITypeSymbol> sourceTypeSymbols) =>
        sourceTypeSymbols.SelectMany(GetAllowedFieldNames);

    private static IEnumerable<string> GetAllowedFieldNames(ITypeSymbol sourceTypeSymbol)
    {
        // consider ctor params on the type itself but not base classes
        var names = sourceTypeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => method is
            {
                MethodKind: MethodKind.Constructor,
                DeclaredAccessibility: Accessibility.Public
            })
            .SelectMany(ctor => ctor.Parameters.Select(param => param.Name));

        var nullableSourceTypeSymbol = sourceTypeSymbol;
        while (nullableSourceTypeSymbol != null)
        {
            var members = nullableSourceTypeSymbol.GetMembers();

            var propNames = members
                .OfType<IPropertySymbol>()
                .Where(prop => prop is
                {
                    IsStatic: false,
                    SetMethod.DeclaredAccessibility: Accessibility.Public
                })
                .Select(propSymbol => propSymbol.Name);

            var fieldNames = members
                .OfType<IFieldSymbol>()
                .Where(field => field is
                {
                    IsConst: false,
                    IsStatic: false,
                    DeclaredAccessibility: Accessibility.Public
                })
                .Select(propSymbol => propSymbol.Name);

            names = names.Concat(propNames).Concat(fieldNames);

            nullableSourceTypeSymbol = nullableSourceTypeSymbol.BaseType;
        }

        return names;
    }

    private static IEnumerable<InvocationExpressionSyntax> GetDeclaredFields(ClassDeclarationSyntax classDeclaration)
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
}
