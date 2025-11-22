using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents an argument in a GraphQL field definition.
/// Wraps calls to .Argument() or .Arguments() methods.
/// </summary>
public sealed class GraphQLFieldArgument
{
    private readonly Lazy<GraphQLObjectProperty<string>?> _nameArgument;
    private readonly Lazy<GraphQLObjectProperty<ITypeSymbol>?> _graphTypeGeneric;
    private readonly Lazy<GraphQLObjectProperty<bool>?> _nullable;
    private readonly Lazy<GraphQLObjectProperty<string>?> _descriptionArgument;
    private readonly Lazy<GraphQLFieldArgumentConfigureAction?> _configureAction;
    private readonly Lazy<Location> _location;
    private readonly Lazy<GraphQLFieldInvocation?> _parentField;

    private GraphQLFieldArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel, GraphQLFieldInvocation? parentField = null)
    {
        Syntax = invocation;
        SemanticModel = semanticModel;

        _nameArgument = new Lazy<GraphQLObjectProperty<string>?>(GetNameArgument);
        _graphTypeGeneric = new Lazy<GraphQLObjectProperty<ITypeSymbol>?>(GetGraphTypeFromGeneric);
        _nullable = new Lazy<GraphQLObjectProperty<bool>?>(GetNullable);
        _descriptionArgument = new Lazy<GraphQLObjectProperty<string>?>(GetDescriptionArgument);
        _configureAction = new Lazy<GraphQLFieldArgumentConfigureAction?>(GetConfigureAction);
        _location = new Lazy<Location>(() => GetLocation(invocation, semanticModel));
        _parentField = new Lazy<GraphQLFieldInvocation?>(() => parentField ?? FindParentField());
    }

    /// <summary>
    /// Gets the 'name' argument from the Argument() method call.
    /// </summary>
    public GraphQLObjectProperty<string>? Name => _nameArgument.Value;

    /// <summary>
    /// Gets the graph type from the generic type argument.
    /// </summary>
    public GraphQLObjectProperty<ITypeSymbol>? GraphTypeGeneric => _graphTypeGeneric.Value;

    /// <summary>
    /// Gets the 'nullable' argument from the Argument() method call.
    /// </summary>
    public GraphQLObjectProperty<bool>? Nullable => _nullable.Value;

    /// <summary>
    /// Gets the 'description' argument from the Argument() method call.
    /// </summary>
    public GraphQLObjectProperty<string>? Description => _descriptionArgument.Value;

    /// <summary>
    /// Gets the 'configure' argument from the Argument() method call.
    /// </summary>
    public GraphQLFieldArgumentConfigureAction? ConfigureAction => _configureAction.Value;

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
    /// Gets the name of the argument, checking the 'configure' action first, then the 'name' argument.
    /// </summary>
    public GraphQLObjectProperty<string>? GetName()
    {
        return ConfigureAction?.Name ?? Name;
    }

    /// <summary>
    /// Gets the graph type of the argument, checking the 'configure' action first, then the generic type.
    /// </summary>
    public GraphQLObjectProperty<ITypeSymbol>? GetGraphType()
    {
        return ConfigureAction?.GraphType ?? GraphTypeGeneric;
    }

    /// <summary>
    /// Gets the description of the argument, checking the 'configure' action first, then the 'description' argument.
    /// </summary>
    public GraphQLObjectProperty<string>? GetDescription()
    {
        return ConfigureAction?.Description ?? Description;
    }

    /// <summary>
    /// Gets the default value of the argument from the configure action.
    /// </summary>
    public GraphQLObjectProperty<object>? GetDefaultValue()
    {
        return ConfigureAction?.DefaultValue;
    }

    /// <summary>
    /// Gets the deprecation reason of the argument from the configure action.
    /// </summary>
    public GraphQLObjectProperty<string>? GetDeprecationReason()
    {
        return ConfigureAction?.DeprecationReason;
    }

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

    private GraphQLObjectProperty<string>? GetNameArgument()
    {
        // Try to get from explicit 'name' argument: .Argument<IntGraphType>("argName") or .Argument<int>("argName", nullable: true)
        var nameArg = GetArgument("name");
        if (nameArg != null)
        {
            switch (nameArg.Expression)
            {
                // .Argument<IntGraphType>("argName")
                case LiteralExpressionSyntax literal:
                {
                    return new GraphQLObjectProperty<string>(literal.Token.ValueText, literal.GetLocation());
                }
                // .Argument<IntGraphType>(ConstArgName)
                case IdentifierNameSyntax or MemberAccessExpressionSyntax:
                {
                    var symbol = SemanticModel.GetSymbolInfo(nameArg.Expression).Symbol;
                    if (symbol is IFieldSymbol { IsConst: true, ConstantValue: string constName })
                    {
                        return new GraphQLObjectProperty<string>(constName, nameArg.Expression.GetLocation());
                    }

                    break;
                }
            }
        }

        return null;
    }

    private GraphQLObjectProperty<ITypeSymbol>? GetGraphTypeFromGeneric()
    {
        // Try to get from generic type argument
        if (Syntax.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName })
        {
            if (genericName.TypeArgumentList.Arguments.Count > 0)
            {
                var genericTypeArg = genericName.TypeArgumentList.Arguments[0];
                var typeInfo = SemanticModel.GetTypeInfo(genericTypeArg);
                if (typeInfo.Type != null)
                {
                    return new GraphQLObjectProperty<ITypeSymbol>(typeInfo.Type, genericTypeArg.GetLocation());
                }
            }
        }

        return null;
    }

    private GraphQLObjectProperty<bool>? GetNullable()
    {
        // Try to get from explicit 'nullable' argument: .Argument<int>("argName", nullable: true)
        var nullableArg = GetArgument("nullable");
        if (nullableArg?.Expression is LiteralExpressionSyntax literal)
        {
            if (literal.Token.Value is bool boolValue)
            {
                return new GraphQLObjectProperty<bool>(boolValue, literal.GetLocation());
            }
        }

        return null;
    }

    private GraphQLObjectProperty<string>? GetDescriptionArgument()
    {
        // Try to get from explicit 'description' argument: .Argument<IntGraphType>("limit", "Description") or .Argument<int>("limit", nullable: true, description: "desc")
        var descArg = GetArgument("description");
        if (descArg?.Expression is LiteralExpressionSyntax literal)
        {
            return new GraphQLObjectProperty<string>(
                literal.Token.ValueText,
                descArg.Expression.GetLocation());
        }

        return null;
    }

    private GraphQLFieldArgumentConfigureAction? GetConfigureAction()
    {
        // Try to get from configure action: .Argument<IntGraphType>("limit", arg => arg.Description = "Description")
        var configureArg = GetArgument("configure");
        if (configureArg?.Expression != null)
        {
            return GraphQLFieldArgumentConfigureAction.TryCreate(configureArg.Expression, SemanticModel);
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
}
