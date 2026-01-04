using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents data inferred from an expression-based field definition.
/// For example: Field(x => x.PropertyName) or Field("name", x => x.PropertyName)
/// </summary>
public sealed class GraphQLFieldExpression
{
    private readonly Lazy<GraphQLObjectProperty<string?>?> _name;
    private readonly Lazy<GraphQLObjectProperty<string?>?> _description;
    private readonly Lazy<GraphQLObjectProperty<string?>?> _deprecationReason;
    private readonly Lazy<GraphQLObjectProperty<object?>?> _defaultValue;

    private GraphQLFieldExpression(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        Syntax = expression;
        SemanticModel = semanticModel;

        _name = new Lazy<GraphQLObjectProperty<string?>?>(GetName);
        _description = new Lazy<GraphQLObjectProperty<string?>?>(GetDescription);
        _deprecationReason = new Lazy<GraphQLObjectProperty<string?>?>(GetDeprecationReason);
        _defaultValue = new Lazy<GraphQLObjectProperty<object?>?>(GetDefaultValue);
    }

    /// <summary>
    /// Gets a value indicating whether the expression is a valid member access expression.
    /// </summary>
    public bool IsValid => GetMemberAccess() != null;

    /// <summary>
    /// Gets the name inferred from the expression (e.g., property name from x => x.PropertyName).
    /// Returns null when the expression is not valid.
    /// </summary>
    public GraphQLObjectProperty<string?>? Name => _name.Value;

    /// <summary>
    /// Gets the description from attributes on the member referenced by the expression.
    /// Returns null when the expression is not valid.
    /// </summary>
    public GraphQLObjectProperty<string?>? Description => _description.Value;

    /// <summary>
    /// Gets the deprecation reason from attributes on the member referenced by the expression.
    /// Returns null when the expression is not valid.
    /// </summary>
    public GraphQLObjectProperty<string?>? DeprecationReason => _deprecationReason.Value;

    /// <summary>
    /// Gets the default value from attributes on the member referenced by the expression.
    /// Returns null when the expression is not valid.
    /// </summary>
    public GraphQLObjectProperty<object?>? DefaultValue => _defaultValue.Value;

    /// <summary>
    /// Gets the underlying expression syntax.
    /// </summary>
    public ExpressionSyntax Syntax { get; }

    /// <summary>
    /// Gets the semantic model used for analysis.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Creates a GraphQLFieldExpression from a lambda expression.
    /// Returns null if the expression is not a lambda expression.
    /// </summary>
    public static GraphQLFieldExpression? TryCreate(ExpressionSyntax? expression, SemanticModel semanticModel)
    {
        if (expression is null)
        {
            return null;
        }

        // Check if it's a lambda expression
        if (expression is not (SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax))
        {
            return null;
        }

        return new GraphQLFieldExpression(expression, semanticModel);
    }

    private GraphQLObjectProperty<string?>? GetName()
    {
        var memberAccess = GetMemberAccess();
        if (memberAccess != null)
        {
            var propertyName = memberAccess.Name.Identifier.Text;
            return new GraphQLObjectProperty<string?>(propertyName, memberAccess.Name.GetLocation());
        }

        return null;
    }

    private GraphQLObjectProperty<string?>? GetDescription()
    {
        var member = GetMemberSymbol();
        if (member == null)
        {
            return null;
        }

        // Look for [Description] attribute
        foreach (var attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass?.Name is "DescriptionAttribute")
            {
                if (attribute.ConstructorArguments.Length > 0 &&
                    attribute.ConstructorArguments[0].Value is string description)
                {
                    var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                    if (location != null)
                    {
                        return new GraphQLObjectProperty<string?>(description, location);
                    }
                }
            }
        }

        return null;
    }

    private GraphQLObjectProperty<string?>? GetDeprecationReason()
    {
        var member = GetMemberSymbol();
        if (member == null)
        {
            return null;
        }

        // Look for [Obsolete] attribute
        foreach (var attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == "ObsoleteAttribute")
            {
                if (attribute.ConstructorArguments.Length > 0 &&
                    attribute.ConstructorArguments[0].Value is string reason)
                {
                    var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                    if (location != null)
                    {
                        return new GraphQLObjectProperty<string?>(reason, location);
                    }
                }
            }
        }

        return null;
    }

    private GraphQLObjectProperty<object?>? GetDefaultValue()
    {
        var member = GetMemberSymbol();
        if (member == null)
        {
            return null;
        }

        // Look for [DefaultAstValue] attribute
        foreach (var attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass?.Name is "DefaultAstValueAttribute")
            {
                if (attribute.ConstructorArguments.Length > 0)
                {
                    var value = attribute.ConstructorArguments[0].Value;
                    var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                    if (location != null)
                    {
                        return new GraphQLObjectProperty<object?>(value, location);
                    }
                }
            }
        }

        return null;
    }

    private ISymbol? GetMemberSymbol()
    {
        var memberAccess = GetMemberAccess();
        if (memberAccess == null)
        {
            return null;
        }

        var symbolInfo = SemanticModel.GetSymbolInfo(memberAccess);
        return symbolInfo.Symbol;
    }

    private MemberAccessExpressionSyntax? GetMemberAccess()
    {
        return Syntax switch
        {
            SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax memberAccess } => memberAccess,
            ParenthesizedLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax memberAccess } => memberAccess,
            _ => null
        };
    }
}
