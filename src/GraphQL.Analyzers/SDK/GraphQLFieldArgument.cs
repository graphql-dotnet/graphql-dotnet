using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents an argument in a GraphQL field definition.
/// Wraps calls to .Argument() or .Arguments() methods.
/// </summary>
public sealed class GraphQLFieldArgument
{
    private readonly Lazy<GraphQLFieldProperty<string>?> _name;
    private readonly Lazy<GraphQLFieldProperty<ITypeSymbol>?> _graphType;
    private readonly Lazy<GraphQLFieldProperty<string>?> _description;
    private readonly Lazy<GraphQLFieldProperty<object>?> _defaultValue;
    private readonly Lazy<Location> _location;
    private readonly Lazy<GraphQLFieldInvocation?> _parentField;

    private GraphQLFieldArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel, GraphQLFieldInvocation? parentField = null)
    {
        Syntax = invocation;
        SemanticModel = semanticModel;

        _name = new Lazy<GraphQLFieldProperty<string>?>(GetArgumentName);
        _graphType = new Lazy<GraphQLFieldProperty<ITypeSymbol>?>(GetGraphType);
        _description = new Lazy<GraphQLFieldProperty<string>?>(GetDescription);
        _defaultValue = new Lazy<GraphQLFieldProperty<object>?>(GetDefaultValue);
        _location = new Lazy<Location>(() => GetLocation(invocation, semanticModel));
        _parentField = new Lazy<GraphQLFieldInvocation?>(() => parentField ?? FindParentField());
    }

    /// <summary>
    /// Gets the name of the argument.
    /// </summary>
    public GraphQLFieldProperty<string>? Name => _name.Value;

    /// <summary>
    /// Gets the graph type of the argument.
    /// </summary>
    public GraphQLFieldProperty<ITypeSymbol>? GraphType => _graphType.Value;

    /// <summary>
    /// Gets the description of the argument.
    /// </summary>
    public GraphQLFieldProperty<string>? Description => _description.Value;

    /// <summary>
    /// Gets the default value of the argument, if specified.
    /// </summary>
    public GraphQLFieldProperty<object>? DefaultValue => _defaultValue.Value;

    /// <summary>
    /// Gets the location of the entire argument definition in source code.
    /// </summary>
    public Location Location => _location.Value;

    /// <summary>
    /// Gets the parent field invocation that this argument belongs to, if it can be determined.
    /// </summary>
    public GraphQLFieldInvocation? ParentField => _parentField.Value;

    /// <summary>
    /// Gets the underlying invocation expression syntax.
    /// </summary>
    public InvocationExpressionSyntax Syntax { get; }

    /// <summary>
    /// Gets the semantic model used for analysis.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Creates a GraphQLFieldArgument from an invocation expression, if it represents an Argument() method call.
    /// </summary>
    public static GraphQLFieldArgument? TryCreate(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (!semanticModel.IsGraphQLMethodInvocation(invocation, "Argument"))
        {
            return null;
        }

        return new GraphQLFieldArgument(invocation, semanticModel);
    }

    /// <summary>
    /// Creates a GraphQLFieldArgument from an invocation expression with a known parent field.
    /// </summary>
    internal static GraphQLFieldArgument? TryCreate(InvocationExpressionSyntax invocation, SemanticModel semanticModel, GraphQLFieldInvocation parentField)
    {
        if (!semanticModel.IsGraphQLMethodInvocation(invocation, "Argument"))
        {
            return null;
        }

        return new GraphQLFieldArgument(invocation, semanticModel, parentField);
    }

    private GraphQLFieldInvocation? FindParentField()
    {
        var current = Syntax.Expression;
        while (current is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax invocation })
        {
            // Try to create a field invocation from this invocation
            var field = GraphQLFieldInvocation.TryCreate(invocation, SemanticModel);
            if (field != null)
            {
                return field;
            }

            // If not a field, continue walking up the chain
            current = invocation.Expression;
        }

        return null;
    }

    private GraphQLFieldProperty<string>? GetArgumentName()
    {
        // Try to get from explicit 'name' argument: .Argument<IntGraphType>("argName")
        var nameArg = GetArgument("name");
        if (nameArg != null)
        {
            switch (nameArg.Expression)
            {
                // .Argument<IntGraphType>("argName")
                case LiteralExpressionSyntax literal:
                {
                    return new GraphQLFieldProperty<string>(literal.Token.ValueText, literal.GetLocation());
                }
                // .Argument<IntGraphType>(ConstArgName)
                case IdentifierNameSyntax or MemberAccessExpressionSyntax:
                {
                    var symbol = SemanticModel.GetSymbolInfo(nameArg.Expression).Symbol;
                    if (symbol is IFieldSymbol { IsConst: true, ConstantValue: string constName })
                    {
                        return new GraphQLFieldProperty<string>(constName, nameArg.Expression.GetLocation());
                    }

                    break;
                }
            }
        }

        return null;
    }

    private GraphQLFieldProperty<ITypeSymbol>? GetGraphType()
    {
        // Try to get from 'type' argument
        var typeArg = GetArgument("type");
        if (typeArg != null)
        {
            var typeInfo = SemanticModel.GetTypeInfo(typeArg.Expression);
            if (typeInfo.Type != null)
            {
                return new GraphQLFieldProperty<ITypeSymbol>(typeInfo.Type, typeArg.Expression.GetLocation());
            }
        }

        // Try to get from generic type argument
        if (Syntax.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName })
        {
            if (genericName.TypeArgumentList.Arguments.Count > 0)
            {
                var genericTypeArg = genericName.TypeArgumentList.Arguments[0];
                var typeInfo = SemanticModel.GetTypeInfo(genericTypeArg);
                if (typeInfo.Type != null)
                {
                    return new GraphQLFieldProperty<ITypeSymbol>(typeInfo.Type, genericTypeArg.GetLocation());
                }
            }
        }

        return null;
    }

    private GraphQLFieldProperty<string>? GetDescription()
    {
        // Try to get from explicit 'description' argument: .Argument<IntGraphType>("limit", "Description")
        var descArg = GetArgument("description");
        if (descArg?.Expression is LiteralExpressionSyntax literal)
        {
            return new GraphQLFieldProperty<string>(
                literal.Token.ValueText,
                descArg.Expression.GetLocation());
        }

        // Try to get from configure action: .Argument<IntGraphType>("limit", arg => arg.Description = "Description")
        var configureArg = GetArgument("configure");
        if (configureArg?.Expression is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax)
        {
            var descriptionAssignment = FindPropertyAssignment<string>(configureArg.Expression, "Description");
            if (descriptionAssignment != null)
            {
                return descriptionAssignment;
            }
        }

        return null;
    }

    private GraphQLFieldProperty<object>? GetDefaultValue()
    {
        // Try to get from configure action: .Argument<IntGraphType>("limit", arg => arg.DefaultValue = 50)
        var configureArg = GetArgument("configure");
        if (configureArg?.Expression is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax)
        {
            var defaultValueAssignment = FindPropertyAssignment<object>(configureArg.Expression, "DefaultValue");
            if (defaultValueAssignment == null)
            {
                return null;
            }

            // Try to get the constant value of the assignment
            var operation = SemanticModel.GetOperation(configureArg.Expression);
            if (operation is not IAnonymousFunctionOperation lambdaOp)
            {
                return null;
            }

            foreach (var assignment in lambdaOp.Body.Descendants().OfType<ISimpleAssignmentOperation>())
            {
                if (assignment.Target is IPropertyReferenceOperation { Property.Name: "DefaultValue" })
                {
                    var valueOperation = assignment.Value;
                    if (valueOperation is IConversionOperation conversion)
                    {
                        valueOperation = conversion.Operand;
                    }
                    var constantValue = valueOperation.ConstantValue;
                    if (constantValue.HasValue)
                    {
                        return new GraphQLFieldProperty<object>(
                            constantValue.Value!,
                            assignment.Value.Syntax.GetLocation());
                    }
                }
            }
        }

        return null;
    }

    private static Location GetLocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return invocation.Expression is MemberAccessExpressionSyntax mem
            // Argument<StringGraphType>("limit")
            ? Location.Create(semanticModel.SyntaxTree, new TextSpan(
                mem.Name.SpanStart,
                invocation.ArgumentList.Span.End - mem.Name.SpanStart))
            // default case
            : invocation.Expression.GetLocation();
    }

    private ArgumentSyntax? GetArgument(string argumentName)
    {
        if (SemanticModel.GetSymbolInfo(Syntax).Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        // Check named arguments
        foreach (var arg in Syntax.ArgumentList.Arguments)
        {
            if (arg.NameColon?.Name.Identifier.Text == argumentName)
            {
                return arg;
            }
        }

        // Check positional arguments
        var paramIndex = Array.FindIndex(methodSymbol.Parameters.ToArray(), p => p.Name == argumentName);
        if (paramIndex >= 0 && paramIndex < Syntax.ArgumentList.Arguments.Count)
        {
            var arg = Syntax.ArgumentList.Arguments[paramIndex];
            // Make sure it's not a named argument for a different parameter
            if (arg.NameColon == null)
            {
                return arg;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a property assignment in a lambda expression (e.g., arg => arg.PropertyName = "value").
    /// </summary>
    private GraphQLFieldProperty<TType>? FindPropertyAssignment<TType>(ExpressionSyntax lambdaExpression, string propertyName)
    {
        // Use semantic model operations for more reliable analysis
        var operation = SemanticModel.GetOperation(lambdaExpression);
        if (operation is not IAnonymousFunctionOperation lambdaOp)
        {
            return null;
        }

        foreach (var assignment in lambdaOp.Body.Descendants().OfType<ISimpleAssignmentOperation>())
        {
            if (assignment.Target is not IPropertyReferenceOperation propRef ||
                propRef.Property.Name != propertyName)
            {
                continue;
            }

            var valueOperation = assignment.Value;
            if (valueOperation is IConversionOperation conversion)
            {
                valueOperation = conversion.Operand;
            }

            var constantValue = valueOperation.ConstantValue;
            if (constantValue is { HasValue: true })
            {
                switch (constantValue.Value)
                {
                    case null:
                        return new GraphQLFieldProperty<TType>(
                            default,
                            assignment.Value.Syntax.GetLocation());
                    case TType value:
                        return new GraphQLFieldProperty<TType>(
                            value,
                            assignment.Value.Syntax.GetLocation());
                }
            }
        }

        return null;
    }
}
