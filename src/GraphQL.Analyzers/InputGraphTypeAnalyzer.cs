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
        messageFormat: "No instance property with public setter, public field or public constructor parameter called '{0}' was found on the '{1}' source type. This field will be ignored during the input deserialization.",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.INVALID_INPUT_FIELD);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(InvalidInputField);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var sourceTypeSymbol = GetSourceTypeSymbol(classDeclaration, context);
        if (sourceTypeSymbol == null)
        {
            return;
        }

        var allowedFieldNames = GetAllowedFieldNames(sourceTypeSymbol);
        var declaredFields = GetDeclaredFields(classDeclaration);

        foreach (var fieldInvocationExpression in declaredFields)
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
            if (!allowedFieldNames.Contains(fieldName))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        InvalidInputField,
                        nameExpression.GetLocation(),
                        fieldName,
                        sourceTypeSymbol.Name));
            }
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

        if (!IsInputObjectGraphType(typeSymbol.BaseType, context))
        {
            return null;
        }

        var baseType = typeSymbol.BaseType;
        ITypeSymbol? sourceTypeSymbol = null;

        while (baseType != null)
        {
            if (!baseType.IsGenericType)
            {
                baseType = baseType.BaseType;
            }
            else
            {
                sourceTypeSymbol = baseType.TypeArguments.FirstOrDefault();
                break;
            }
        }

        // we currently don't support constructs like MyInputType<TSource> : InputObjectGraphType<TSource>
        if (sourceTypeSymbol is ITypeParameterSymbol)
        {
            return null;
        }

        if (sourceTypeSymbol?.SpecialType == SpecialType.System_Object)
        {
            return null;
        }

        return sourceTypeSymbol;
    }

    private static bool IsInputObjectGraphType(
        INamedTypeSymbol? typeSymbol,
        SyntaxNodeAnalysisContext context)
    {
        // quick check for interface implementation before iterating on base classes
        if (typeSymbol?.AllInterfaces.Any(i => i.Name == Constants.Interfaces.IInputObjectGraphType) != true)
        {
            return false;
        }

        var genericInputObjectGraphType = context.Compilation.GetTypeByMetadataName("GraphQL.Types.InputObjectGraphType`1");

        while (typeSymbol != null)
        {
            if (SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, genericInputObjectGraphType))
            {
                return true;
            }

            typeSymbol = typeSymbol.BaseType;
        }

        return false;
    }

    private static ImmutableHashSet<string> GetAllowedFieldNames(ITypeSymbol sourceTypeSymbol)
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

        return names.ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);
    }

    private static IEnumerable<InvocationExpressionSyntax> GetDeclaredFields(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsFieldInvocation)
            .Where(exp => !IsDeprecated(exp))
            .ToList();

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

    private static bool IsDeprecated(SyntaxNode fieldExpression) =>
        fieldExpression.Ancestors()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(m => m.Name.Identifier.Text == Constants.MethodNames.DeprecationReason);
}
