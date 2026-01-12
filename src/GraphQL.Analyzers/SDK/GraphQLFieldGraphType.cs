using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents the graph type of a field with its properties.
/// Provides information about type modifiers (List, NonNull) and the unwrapped core type.
/// </summary>
public sealed class GraphQLFieldGraphType
{
    private readonly SemanticModel _semanticModel;
    private readonly Lazy<GraphQLGraphType?> _unwrappedType;

    internal GraphQLFieldGraphType(ITypeSymbol typeSymbol, Location location, SemanticModel semanticModel)
    {
        TypeSymbol = typeSymbol;
        Location = location;
        _semanticModel = semanticModel;

        // Analyze the type to determine if it's a list and/or nullable
        (IsList, IsNullable) = AnalyzeTypeModifiers(typeSymbol, out var unwrappedSymbol);
        UnwrappedTypeSymbol = unwrappedSymbol;

        _unwrappedType = new Lazy<GraphQLGraphType?>(FindUnwrappedGraphType);
    }

    /// <summary>
    /// Gets the type symbol of the field's graph type.
    /// </summary>
    public ITypeSymbol TypeSymbol { get; }

    /// <summary>
    /// Gets the location of the graph type in source code.
    /// </summary>
    public Location Location { get; }

    /// <summary>
    /// Gets whether the field type is wrapped in a ListGraphType.
    /// </summary>
    public bool IsList { get; }

    /// <summary>
    /// Gets whether the field type is nullable (not wrapped in NonNullGraphType).
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the unwrapped type symbol (after removing List and NonNull wrappers).
    /// </summary>
    public ITypeSymbol UnwrappedTypeSymbol { get; }

    /// <summary>
    /// Gets the unwrapped GraphQL graph type after stripping all ListGraphType and NonNullGraphType wrappers.
    /// Returns null if the unwrapped type is not a graph type or cannot be found.
    /// </summary>
    /// <returns>The unwrapped GraphQLGraphType instance, or null if not found.</returns>
    public GraphQLGraphType? GetUnwrappedType() => _unwrappedType.Value;

    private (bool isList, bool isNullable) AnalyzeTypeModifiers(ITypeSymbol typeSymbol, out ITypeSymbol unwrappedSymbol)
    {
        bool isList = false;
        bool isNullable = true; // By default, types are nullable unless wrapped in NonNullGraphType

        var currentType = typeSymbol;

        // Strip NonNullGraphType wrappers
        while (IsNonNullGraphType(currentType))
        {
            isNullable = false;
            currentType = GetWrappedType(currentType);
        }

        // Strip ListGraphType wrapper
        if (IsListGraphType(currentType))
        {
            isList = true;
            currentType = GetWrappedType(currentType);

            // Check if the list element type is wrapped in NonNullGraphType
            if (IsNonNullGraphType(currentType))
            {
                currentType = GetWrappedType(currentType);
            }
        }

        unwrappedSymbol = currentType;
        return (isList, isNullable);
    }

    private bool IsNonNullGraphType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return false;

        return namedType.Name == "NonNullGraphType" || namedType.ConstructedFrom.Name == "NonNullGraphType";
    }

    private bool IsListGraphType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return false;

        return namedType.Name == "ListGraphType" || namedType.ConstructedFrom.Name == "ListGraphType";
    }

    private ITypeSymbol GetWrappedType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return typeSymbol;

        // For generic types like NonNullGraphType<T> or ListGraphType<T>, get T
        if (namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0];
        }

        // For non-generic types, try to get ResolvedType property
        var resolvedTypeProperty = namedType.GetMembers("ResolvedType")
            .OfType<IPropertySymbol>()
            .FirstOrDefault();

        return resolvedTypeProperty?.Type ?? typeSymbol;
    }

    private GraphQLGraphType? FindUnwrappedGraphType()
    {
        // Try to find the class declaration for the unwrapped type symbol
        var syntaxReferences = UnwrappedTypeSymbol.DeclaringSyntaxReferences;

        foreach (var syntaxRef in syntaxReferences)
        {
            var syntaxNode = syntaxRef.GetSyntax();

            if (syntaxNode is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classDeclaration)
            {
                var graphType = GraphQLGraphType.TryCreate(classDeclaration, _semanticModel);
                if (graphType != null)
                {
                    return graphType;
                }
            }
        }

        return null;
    }
}
