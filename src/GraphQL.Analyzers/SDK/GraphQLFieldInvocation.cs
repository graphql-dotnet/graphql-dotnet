using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents a Field() method invocation in GraphQL.NET code.
/// Provides easy access to field properties like name, type, arguments, etc.
/// </summary>
public sealed class GraphQLFieldInvocation
{
    private readonly Lazy<GraphQLFieldProperty<string>?> _name;
    private readonly Lazy<GraphQLFieldProperty<ITypeSymbol>?> _graphType;
    private readonly Lazy<GraphQLFieldProperty<string>?> _description;
    private readonly Lazy<GraphQLFieldProperty<string>?> _deprecationReason;
    private readonly Lazy<GraphQLFieldProperty<ExpressionSyntax>?> _resolverExpression;
    private readonly Lazy<IReadOnlyList<GraphQLFieldArgument>> _arguments;
    private readonly Lazy<GraphQLGraphType?> _declaringGraphType;
    private readonly Lazy<Location> _location;

    private GraphQLFieldInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        Syntax = invocation;
        SemanticModel = semanticModel;

        _name = new Lazy<GraphQLFieldProperty<string>?>(GetFieldName);
        _graphType = new Lazy<GraphQLFieldProperty<ITypeSymbol>?>(GetGraphType);
        _description = new Lazy<GraphQLFieldProperty<string>?>(GetDescription);
        _deprecationReason = new Lazy<GraphQLFieldProperty<string>?>(GetDeprecationReason);
        _resolverExpression = new Lazy<GraphQLFieldProperty<ExpressionSyntax>?>(GetResolverExpression);
        _arguments = new Lazy<IReadOnlyList<GraphQLFieldArgument>>(GetArguments);
        _declaringGraphType = new Lazy<GraphQLGraphType?>(FindDeclaringGraphType);
        _location = new Lazy<Location>(invocation.GetLocation);
    }

    /// <summary>
    /// Gets the name of the field with its location, if it can be determined.
    /// </summary>
    public GraphQLFieldProperty<string>? Name => _name.Value;

    /// <summary>
    /// Gets the graph type of the field with its location, if it can be determined.
    /// </summary>
    public GraphQLFieldProperty<ITypeSymbol>? GraphType => _graphType.Value;

    /// <summary>
    /// Gets the description of the field with its location, if specified.
    /// </summary>
    public GraphQLFieldProperty<string>? Description => _description.Value;

    /// <summary>
    /// Gets the deprecation reason of the field with its location, if specified.
    /// </summary>
    public GraphQLFieldProperty<string>? DeprecationReason => _deprecationReason.Value;

    /// <summary>
    /// Gets the resolver expression (lambda or delegate) with its location, if specified.
    /// </summary>
    public GraphQLFieldProperty<ExpressionSyntax>? ResolverExpression => _resolverExpression.Value;

    /// <summary>
    /// Gets the arguments defined for this field.
    /// </summary>
    public IReadOnlyList<GraphQLFieldArgument> Arguments => _arguments.Value;

    /// <summary>
    /// Gets the graph type that declares this field, if it can be determined.
    /// </summary>
    public GraphQLGraphType? DeclaringGraphType => _declaringGraphType.Value;

    /// <summary>
    /// Gets the location of the entire field invocation in source code.
    /// </summary>
    public Location Location => _location.Value;

    /// <summary>
    /// Gets the underlying invocation expression syntax.
    /// </summary>
    public InvocationExpressionSyntax Syntax { get; }

    /// <summary>
    /// Gets the semantic model used for analysis.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Creates a GraphQLFieldInvocation from an invocation expression, if it represents a Field() method call.
    /// </summary>
    public static GraphQLFieldInvocation? TryCreate(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (!semanticModel.IsGraphQLMethodInvocation(invocation, "Field"))
        {
            return null;
        }

        return new GraphQLFieldInvocation(invocation, semanticModel);
    }

    private GraphQLFieldProperty<string>? GetFieldName()
    {
        // Try to get from explicit 'name' argument: Field<StringGraphType>("fieldName")
        var nameArg = GetArgument("name");
        if (nameArg != null)
        {
            switch (nameArg.Expression)
            {
                case LiteralExpressionSyntax literal:
                    return new GraphQLFieldProperty<string>(literal.Token.ValueText, literal.GetLocation());
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

        // Try to infer from 'expression' argument: Field(x => x.PropertyName)
        var expressionArg = GetArgument("expression");
        if (expressionArg?.Expression is SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax memberAccess })
        {
            var propertyName = memberAccess.Name.Identifier.Text;
            return new GraphQLFieldProperty<string>(propertyName, memberAccess.Name.GetLocation());
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
        if (Syntax.Expression is GenericNameSyntax { TypeArgumentList.Arguments.Count: > 0 } genericName)
        {
            var genericTypeArg = genericName.TypeArgumentList.Arguments[0];
            var typeInfo = SemanticModel.GetTypeInfo(genericTypeArg);
            if (typeInfo.Type != null)
            {
                return new GraphQLFieldProperty<ITypeSymbol>(typeInfo.Type, genericTypeArg.GetLocation());
            }
        }

        return null;
    }

    private GraphQLFieldProperty<string>? GetDescription()
    {
        var descriptionCall = FindChainedMethod("Description");
        var arg = descriptionCall?.ArgumentList.Arguments.FirstOrDefault();
        if (arg?.Expression is LiteralExpressionSyntax literal)
        {
            return new GraphQLFieldProperty<string>(
                literal.Token.ValueText,
                arg.Expression.GetLocation());
        }

        return null;
    }

    private GraphQLFieldProperty<string>? GetDeprecationReason()
    {
        var deprecationCall = FindChainedMethod("DeprecationReason");
        var arg = deprecationCall?.ArgumentList.Arguments.FirstOrDefault();
        if (arg?.Expression is LiteralExpressionSyntax literal)
        {
            return new GraphQLFieldProperty<string>(
                literal.Token.ValueText,
                arg.Expression.GetLocation());
        }

        return null;
    }

    private GraphQLFieldProperty<ExpressionSyntax>? GetResolverExpression()
    {
        // .Resolve(), .ResolveAsync(), or .ResolveDelegate()
        var resolveCall = FindChainedMethod("Resolve")
            ?? FindChainedMethod("ResolveAsync")
            ?? FindChainedMethod("ResolveDelegate");

        var arg = resolveCall?.ArgumentList.Arguments.FirstOrDefault();
        if (arg?.Expression != null)
        {
            return new GraphQLFieldProperty<ExpressionSyntax>(
                arg.Expression,
                arg.Expression.GetLocation());
        }

        return null;
    }

    private IReadOnlyList<GraphQLFieldArgument> GetArguments()
    {
        var result = new List<GraphQLFieldArgument>();

        var current = Syntax.Parent;
        while (current is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Parent is InvocationExpressionSyntax chainedInvocation)
            {
                if (memberAccess.Name.Identifier.Text is "Argument")
                {
                    var arg = GraphQLFieldArgument.TryCreate(chainedInvocation, SemanticModel, this);
                    if (arg != null)
                    {
                        result.Add(arg);
                    }
                }
                current = chainedInvocation.Parent;
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private GraphQLGraphType? FindDeclaringGraphType()
    {
        var classDeclaration = Syntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDeclaration == null)
        {
            return null;
        }

        return GraphQLGraphType.TryCreate(classDeclaration, SemanticModel);
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

    private InvocationExpressionSyntax? FindChainedMethod(string methodName)
    {
        var current = Syntax.Parent;
        while (current != null)
        {
            if (current is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name.Identifier.Text == methodName &&
                    memberAccess.Parent is InvocationExpressionSyntax invocation)
                {
                    return invocation;
                }
                current = memberAccess.Parent;
            }
            else if (current is InvocationExpressionSyntax)
            {
                current = current.Parent;
            }
            else
            {
                break;
            }
        }

        return null;
    }
}
