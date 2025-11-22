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
    private readonly Lazy<GraphQLFieldProperty<string>?> _name;
    private readonly Lazy<GraphQLFieldProperty<ITypeSymbol>?> _graphType;
    private readonly Lazy<GraphQLFieldProperty<string>?> _description;
    private readonly Lazy<GraphQLFieldProperty<object>?> _defaultValue;
    private readonly Lazy<GraphQLFieldProperty<string>?> _deprecationReason;

    private GraphQLFieldArgumentConfigureAction(ExpressionSyntax lambdaExpression, SemanticModel semanticModel)
    {
        Syntax = lambdaExpression;
        SemanticModel = semanticModel;

        _name = new Lazy<GraphQLFieldProperty<string>?>(GetName);
        _graphType = new Lazy<GraphQLFieldProperty<ITypeSymbol>?>(GetGraphType);
        _description = new Lazy<GraphQLFieldProperty<string>?>(GetDescription);
        _defaultValue = new Lazy<GraphQLFieldProperty<object>?>(GetDefaultValue);
        _deprecationReason = new Lazy<GraphQLFieldProperty<string>?>(GetDeprecationReason);
    }

    /// <summary>
    /// Gets the name property from the configure action.
    /// </summary>
    public GraphQLFieldProperty<string>? Name => _name.Value;

    /// <summary>
    /// Gets the graph type property from the configure action.
    /// </summary>
    public GraphQLFieldProperty<ITypeSymbol>? GraphType => _graphType.Value;

    /// <summary>
    /// Gets the description property from the configure action.
    /// </summary>
    public GraphQLFieldProperty<string>? Description => _description.Value;

    /// <summary>
    /// Gets the default value property from the configure action.
    /// </summary>
    public GraphQLFieldProperty<object>? DefaultValue => _defaultValue.Value;

    /// <summary>
    /// Gets the deprecation reason property from the configure action.
    /// </summary>
    public GraphQLFieldProperty<string>? DeprecationReason => _deprecationReason.Value;

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

    private GraphQLFieldProperty<string>? GetName()
    {
        return FindPropertyAssignment<string>("Name");
    }

    private GraphQLFieldProperty<ITypeSymbol>? GetGraphType()
    {
        // For QueryArgument, the type is typically set via the Type or ResolvedType property
        var typeAssignment = FindPropertyAssignment<ITypeSymbol>("Type");
        if (typeAssignment != null)
        {
            return typeAssignment;
        }

        return FindPropertyAssignment<ITypeSymbol>("ResolvedType");
    }

    private GraphQLFieldProperty<string>? GetDescription()
    {
        return FindPropertyAssignment<string>("Description");
    }

    private GraphQLFieldProperty<object>? GetDefaultValue()
    {
        var defaultValueAssignment = FindPropertyAssignment<object>("DefaultValue");
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
                    return new GraphQLFieldProperty<object>(
                        constantValue.Value,
                        assignment.Value.Syntax.GetLocation());
                }
            }
        }

        return null;
    }

    private GraphQLFieldProperty<string>? GetDeprecationReason()
    {
        return FindPropertyAssignment<string>("DeprecationReason");
    }

    /// <summary>
    /// Finds a property assignment in a lambda expression (e.g., arg => arg.PropertyName = "value").
    /// </summary>
    private GraphQLFieldProperty<TType>? FindPropertyAssignment<TType>(string propertyName)
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
