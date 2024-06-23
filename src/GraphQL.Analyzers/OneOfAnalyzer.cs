using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OneOfAnalyzer : DiagnosticAnalyzer
{
    private const string IS_ONE_OF_PROPERTY_NAME = "IsOneOf";

    public static readonly DiagnosticDescriptor OneOfFieldsMustBeNullable = new(
        id: DiagnosticIds.ONE_OF_FIELDS_MUST_BE_NULLABLE,
        title: "OneOf fields must be nullable",
        messageFormat: "All fields of the OneOf input type must be nullable",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.ONE_OF_FIELDS_MUST_BE_NULLABLE);

    public static readonly DiagnosticDescriptor OneOfFieldsMustNotHaveDefaultValue = new(
        id: DiagnosticIds.ONE_OF_FIELDS_MUST_NOT_HAVE_DEFAULT_VALUE,
        title: "OneOf fields must not have default value",
        messageFormat: "OneOf fields must not have default value",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.ONE_OF_FIELDS_MUST_NOT_HAVE_DEFAULT_VALUE);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(OneOfFieldsMustBeNullable, OneOfFieldsMustNotHaveDefaultValue);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeCodeFirst, SyntaxKind.IdentifierName);
        context.RegisterSyntaxNodeAction(AnalyzeTypeFirst, SyntaxKind.Attribute);
    }

    private void AnalyzeCodeFirst(SyntaxNodeAnalysisContext context)
    {
        var name = (IdentifierNameSyntax)context.Node;

        if (!IsOneOfTrue(context, name))
        {
            return;
        }

        var enclosingMethod = name.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
        var statements = enclosingMethod?.Body?.Statements;
        if (statements == null)
        {
            return;
        }

        foreach (var statement in statements)
        {
            var fieldExpression = statement
                .DescendantNodesAndSelf()
                .OfType<SimpleNameSyntax>()
                .FirstOrDefault(nameSyntax =>
                    nameSyntax.Identifier.Text == Constants.MethodNames.Field &&
                    nameSyntax.AncestorsAndSelf()
                        .OfType<ExpressionSyntax>()
                        .FirstOrDefault()
                        ?.IsGraphQLSymbol(context.SemanticModel) == true);

            if (fieldExpression == null)
            {
                continue;
            }

            AnalyzeCodeFirstNullability(context, fieldExpression);
            AnalyzeCodeFirstDefaultValue(context, fieldExpression);
        }
    }

    private static void AnalyzeCodeFirstNullability(SyntaxNodeAnalysisContext context, SimpleNameSyntax fieldExpression)
    {
        // Field<NonNullGraphType<StringGraphType>>("name");
        if (fieldExpression is GenericNameSyntax genericField)
        {
            if (genericField.TypeArgumentList
                    .Arguments
                    .FirstOrDefault()?.ToString()
                    .StartsWith(Constants.Types.NonNullGraphType) == true)
            {
                ReportMustBeNullable(context, fieldExpression.GetLocation());
            }
        }
        else
        {
            var fieldInvocation = fieldExpression.FindMethodInvocationExpression();
            if (fieldInvocation == null)
            {
                return;
            }

            var typeArg = fieldInvocation.GetMethodArgument(Constants.ArgumentNames.Type, context.SemanticModel);

            // Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
            if (typeArg != null)
            {
                if (typeArg.Expression is TypeOfExpressionSyntax { Type: GenericNameSyntax g } &&
                    g.ToString().StartsWith(Constants.Types.NonNullGraphType))
                {
                    ReportMustBeNullable(context, fieldExpression.GetLocation());
                }
            }
            else
            {
                var nullableArg = fieldInvocation.GetMethodArgument(Constants.ArgumentNames.Nullable, context.SemanticModel);

                // Field(x => x.Name);
                if (nullableArg == null)
                {
                    ReportMustBeNullable(context, fieldExpression.GetLocation());
                }
                // Field(x => x.Name, nullable: false);
                else if (nullableArg.Expression is LiteralExpressionSyntax literal &&
                         literal.IsKind(SyntaxKind.FalseLiteralExpression))
                {
                    ReportMustBeNullable(context, fieldExpression.GetLocation());
                }
            }
        }
    }

    private static void AnalyzeCodeFirstDefaultValue(SyntaxNodeAnalysisContext context, SimpleNameSyntax fieldExpression)
    {
        var defaultValueExp = fieldExpression
            .Ancestors()
            .OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(mem => mem.Name.Identifier.Text == Constants.MethodNames.DefaultValue);

        // Field(...).DefaultValue(...);
        if (defaultValueExp != null)
        {
            ReportMustNotHaveDefaultValue(context, defaultValueExp.Name.GetLocation());
        }
    }

    private void AnalyzeTypeFirst(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        if (attribute.Name.ToString()
            is not Constants.AttributeNames.OneOf
            and not Constants.AttributeNames.OneOf + Constants.AttributeNames.Attribute)
        {
            return;
        }

        if (!attribute.IsGraphQLSymbol(context.SemanticModel))
        {
            return;
        }

        var props = attribute
            .Ancestors()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault()
            ?.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>();

        if (props == null)
        {
            return;
        }

        foreach (var prop in props)
        {
            if (HasIgnoreAttribute(prop, context.SemanticModel))
            {
                continue;
            }

            AnalyzeTypeFirstNullability(context, prop);
            AnalyzeTypeFirstDefaultValue(context, prop);
        }
    }

    private static void AnalyzeTypeFirstNullability(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax prop)
    {
        if (prop.Type is NullableTypeSyntax)
        {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(prop.Type);
        if (typeInfo.Type?.IsValueType == true)
        {
            ReportMustBeNullable(context, prop.Type.GetLocation());
            return;
        }

        var nullableContext = context.SemanticModel.GetNullableContext(prop.Type.GetLocation().SourceSpan.Start);
        if (nullableContext == NullableContext.Enabled)
        {
            ReportMustBeNullable(context, prop.Type.GetLocation());
        }
    }

    private static void AnalyzeTypeFirstDefaultValue(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax prop)
    {
        if (prop.Initializer != null)
        {
            ReportMustNotHaveDefaultValue(context, prop.Initializer.Value.GetLocation());
        }
    }

    private static bool IsOneOfTrue(SyntaxNodeAnalysisContext context, IdentifierNameSyntax name)
    {
        if (name.ToString() != IS_ONE_OF_PROPERTY_NAME)
        {
            return false;
        }

        if (!name.IsGraphQLSymbol(context.SemanticModel))
        {
            return false;
        }

        var enclosingSymbol = context.SemanticModel.GetSymbolInfo(name);
        if (enclosingSymbol.Symbol is not IPropertySymbol prop)
        {
            return false;
        }

        if (prop.ContainingType.AllInterfaces.All(i => i.Name != Constants.Interfaces.IInputObjectGraphType))
        {
            return false;
        }

        var assignment = name.Ancestors().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
        return assignment?.Right is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.TrueLiteralExpression);
    }

    private static bool HasIgnoreAttribute(PropertyDeclarationSyntax prop, SemanticModel semanticModel) =>
        prop.AttributeLists.Any(attributes =>
            attributes.Attributes.Any(attribute =>
                attribute.Name.ToString()
                    is Constants.AttributeNames.Ignore
                    or Constants.AttributeNames.Ignore + Constants.AttributeNames.Attribute
                && attribute.IsGraphQLSymbol(semanticModel)));

    private static void ReportMustBeNullable(SyntaxNodeAnalysisContext context, Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(OneOfFieldsMustBeNullable, location));

    private static void ReportMustNotHaveDefaultValue(SyntaxNodeAnalysisContext context, Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(OneOfFieldsMustNotHaveDefaultValue, location));
}
