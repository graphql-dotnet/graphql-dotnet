using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents a configuration action passed to .Argument() methods.
/// Wraps lambda expressions like: arg => arg.Description = "text"
/// </summary>
public sealed class GraphQLFieldArgumentConfigureAction
{
    private readonly Lazy<GraphQLObjectProperty<string?>?> _name;
    private readonly Lazy<GraphQLObjectProperty<ITypeSymbol>?> _graphType;
    private readonly Lazy<GraphQLObjectProperty<string?>?> _description;
    private readonly Lazy<GraphQLObjectProperty<object?>?> _defaultValue;
    private readonly Lazy<GraphQLObjectProperty<string?>?> _deprecationReason;

    private GraphQLFieldArgumentConfigureAction(ExpressionSyntax lambdaExpression, SemanticModel semanticModel)
    {
        Syntax = lambdaExpression;
        SemanticModel = semanticModel;

        _name = new Lazy<GraphQLObjectProperty<string?>?>(GetName);
        _graphType = new Lazy<GraphQLObjectProperty<ITypeSymbol>?>(GetGraphType);
        _description = new Lazy<GraphQLObjectProperty<string?>?>(GetDescription);
        _defaultValue = new Lazy<GraphQLObjectProperty<object?>?>(GetDefaultValue);
        _deprecationReason = new Lazy<GraphQLObjectProperty<string?>?>(GetDeprecationReason);
    }

    /// <summary>
    /// Gets the name property from the configure action.
    /// </summary>
    public GraphQLObjectProperty<string?>? Name => _name.Value;

    /// <summary>
    /// Gets the graph type property from the configure action.
    /// </summary>
    public GraphQLObjectProperty<ITypeSymbol>? GraphType => _graphType.Value;

    /// <summary>
    /// Gets the description property from the configure action.
    /// </summary>
    public GraphQLObjectProperty<string?>? Description => _description.Value;

    /// <summary>
    /// Gets the default value property from the configure action.
    /// </summary>
    public GraphQLObjectProperty<object?>? DefaultValue => _defaultValue.Value;

    /// <summary>
    /// Gets the deprecation reason property from the configure action.
    /// </summary>
    public GraphQLObjectProperty<string?>? DeprecationReason => _deprecationReason.Value;

    /// <summary>
    /// Gets the underlying lambda expression syntax.
    /// </summary>
    public ExpressionSyntax Syntax { get; }

    /// <summary>
    /// Gets the semantic model used for analysis.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Creates a GraphQLFieldArgumentConfigureAction from a lambda expression.
    /// </summary>
    public static GraphQLFieldArgumentConfigureAction? TryCreate(ExpressionSyntax? lambdaExpression, SemanticModel semanticModel)
    {
        if (lambdaExpression is null)
        {
            return null;
        }

        if (lambdaExpression is not (SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax))
        {
            return null;
        }

        return new GraphQLFieldArgumentConfigureAction(lambdaExpression, semanticModel);
    }

    private GraphQLObjectProperty<string?>? GetName()
    {
        return FindPropertyAssignment<string?>("Name");
    }

    private GraphQLObjectProperty<ITypeSymbol>? GetGraphType()
    {
        // For QueryArgument, the type is typically set via the Type or ResolvedType property
        var typeAssignment = FindPropertyAssignment<ITypeSymbol>("Type");
        if (typeAssignment != null)
        {
            return typeAssignment;
        }

        return FindPropertyAssignment<ITypeSymbol>("ResolvedType");
    }

    private GraphQLObjectProperty<string?>? GetDescription()
    {
        return FindPropertyAssignment<string?>("Description");
    }

    private GraphQLObjectProperty<object?>? GetDefaultValue()
    {
        var defaultValueAssignment = FindPropertyAssignment<object?>("DefaultValue");
        if (defaultValueAssignment == null)
        {
            return null;
        }

        // Try to get the constant value of the assignment
        var operation = SemanticModel.GetOperation(Syntax);
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
                    return new GraphQLObjectProperty<object?>(
                        constantValue.Value,
                        assignment.Value.Syntax.GetLocation());
                }
            }
        }

        return null;
    }

    private GraphQLObjectProperty<string?>? GetDeprecationReason()
    {
        return FindPropertyAssignment<string?>("DeprecationReason");
    }

    /// <summary>
    /// Finds a property assignment in a lambda expression (e.g., arg => arg.PropertyName = "value").
    /// </summary>
    private GraphQLObjectProperty<TType>? FindPropertyAssignment<TType>(string propertyName)
    {
        // Use semantic model operations for more reliable analysis
        var operation = SemanticModel.GetOperation(Syntax);
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
                return new GraphQLObjectProperty<TType>(
                    constantValue.Value,
                    assignment.Value.Syntax.GetLocation());
            }
        }

        return null;
    }
}
