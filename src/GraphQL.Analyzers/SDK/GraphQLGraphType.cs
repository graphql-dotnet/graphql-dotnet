using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents a GraphQL graph type (ObjectGraphType, InputObjectGraphType, etc.).
/// Provides access to declared fields and type properties.
/// </summary>
public sealed class GraphQLGraphType
{
    private readonly Lazy<INamedTypeSymbol?> _typeSymbol;
    private readonly Lazy<GraphQLSourceType?> _sourceType;
    private readonly Lazy<IReadOnlyList<GraphQLFieldInvocation>> _fields;
    private readonly Lazy<bool> _isInputType;
    private readonly Lazy<bool> _isOutputType;
    private readonly Lazy<Location> _location;

    private GraphQLGraphType(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        Syntax = classDeclaration;
        SemanticModel = semanticModel;

        _typeSymbol = new Lazy<INamedTypeSymbol?>(() =>
            semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol);
        _sourceType = new Lazy<GraphQLSourceType?>(GetSourceType);
        _fields = new Lazy<IReadOnlyList<GraphQLFieldInvocation>>(GetFields);
        _isInputType = new Lazy<bool>(CheckIsInputType);
        _isOutputType = new Lazy<bool>(CheckIsOutputType);
        _location = new Lazy<Location>(classDeclaration.Identifier.GetLocation);
    }

    /// <summary>
    /// Creates a GraphQLGraphType from a class declaration, if it represents a GraphQL graph type.
    /// </summary>
    public static GraphQLGraphType? TryCreate(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!IsGraphType(typeSymbol))
        {
            return null;
        }

        return new GraphQLGraphType(classDeclaration, semanticModel);
    }

    /// <summary>
    /// Gets the underlying class declaration syntax.
    /// </summary>
    public ClassDeclarationSyntax Syntax { get; }

    /// <summary>
    /// Gets the semantic model used for analysis.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Gets the type symbol of the graph type class.
    /// </summary>
    public INamedTypeSymbol? TypeSymbol => _typeSymbol.Value;

    /// <summary>
    /// Gets the source type (TSourceType generic parameter) if available.
    /// </summary>
    public GraphQLSourceType? SourceType => _sourceType.Value;

    /// <summary>
    /// Gets all fields declared in this graph type.
    /// </summary>
    public IReadOnlyList<GraphQLFieldInvocation> Fields => _fields.Value;

    /// <summary>
    /// Gets whether this is an input graph type (IInputObjectGraphType).
    /// </summary>
    public bool IsInputType => _isInputType.Value;

    /// <summary>
    /// Gets whether this is an output graph type (IObjectGraphType, IInterfaceGraphType).
    /// </summary>
    public bool IsOutputType => _isOutputType.Value;

    /// <summary>
    /// Gets the location of the graph type declaration in source code.
    /// </summary>
    public Location Location => _location.Value;

    /// <summary>
    /// Gets the name of the graph type class.
    /// </summary>
    public string Name => Syntax.Identifier.Text;

    private static bool IsGraphType(INamedTypeSymbol typeSymbol)
    {
        // Check if it implements IGraphType or derives from a type that does
        if (typeSymbol.AllInterfaces.Any(i => i.Name is "IGraphType" or "IComplexGraphType"))
        {
            return true;
        }

        // Check if base type contains "GraphType"
        var baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name.Contains("GraphType"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    private GraphQLSourceType? GetSourceType()
    {
        var typeSymbol = _typeSymbol.Value;
        if (typeSymbol == null)
        {
            return null;
        }

        // Look for ComplexGraphType<TSourceType> base type in syntax
        var baseTypeSyntax = Syntax.BaseList?.Types.FirstOrDefault();
        Location? sourceTypeLocation = null;

        if (baseTypeSyntax?.Type is GenericNameSyntax { TypeArgumentList.Arguments.Count: > 0 } genericName)
        {
            // Get the location of the first type argument (the source type)
            sourceTypeLocation = genericName.TypeArgumentList.Arguments[0].GetLocation();
        }

        // Look for the type symbol in the base type hierarchy
        var baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            switch (baseType.Name)
            {
                case "ComplexGraphType" when baseType is { TypeArguments.Length: > 0 }:
                case "ObjectGraphType" when baseType is { TypeArguments.Length: > 0 }:
                case "InputObjectGraphType" when baseType is { TypeArguments.Length: > 0 }:
                    return new GraphQLSourceType(baseType.TypeArguments[0], sourceTypeLocation);
                default:
                    baseType = baseType.BaseType;
                    break;
            }
        }

        return null;
    }

    private List<GraphQLFieldInvocation> GetFields()
    {
        var fields = new List<GraphQLFieldInvocation>();

        // Find all Field() invocations in the class
        foreach (var invocation in Syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var field = GraphQLFieldInvocation.TryCreate(invocation, SemanticModel);
            if (field != null)
            {
                fields.Add(field);
            }
        }

        return fields;
    }

    private bool CheckIsInputType()
    {
        var typeSymbol = _typeSymbol.Value;
        return typeSymbol?.AllInterfaces.Any(i => i.Name == "IInputObjectGraphType") == true;
    }

    private bool CheckIsOutputType()
    {
        var typeSymbol = _typeSymbol.Value;
        return typeSymbol?.AllInterfaces.Any(i => i.Name is "IObjectGraphType" or "IInterfaceGraphType") == true;
    }

    /// <summary>
    /// Gets a field by name.
    /// </summary>
    public GraphQLFieldInvocation? GetField(string name)
    {
        return Fields.FirstOrDefault(f =>
        {
            var fieldName = f.GetName();
            return fieldName != null && string.Equals(fieldName.Value, name, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Checks if the graph type has a field with the specified name.
    /// </summary>
    public bool HasField(string name)
    {
        return GetField(name) != null;
    }
}
