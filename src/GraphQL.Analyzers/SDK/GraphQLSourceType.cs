using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents the source type (TSourceType) of a GraphQL graph type with its members.
/// </summary>
public sealed class GraphQLSourceType
{
    private readonly Lazy<IReadOnlyList<GraphQLSourceTypeMember>> _members;

    internal GraphQLSourceType(ITypeSymbol typeSymbol)
    {
        TypeSymbol = typeSymbol;
        _members = new Lazy<IReadOnlyList<GraphQLSourceTypeMember>>(GetMembers);
    }

    /// <summary>
    /// Gets the type symbol of the source type.
    /// </summary>
    public ITypeSymbol TypeSymbol { get; }

    /// <summary>
    /// Gets all fields and properties of the source type.
    /// </summary>
    public IReadOnlyList<GraphQLSourceTypeMember> Members => _members.Value;

    /// <summary>
    /// Gets the name of the source type.
    /// </summary>
    public string Name => TypeSymbol.Name;

    private List<GraphQLSourceTypeMember> GetMembers()
    {
        var members = new List<GraphQLSourceTypeMember>();

        // Get all properties and fields that are public or internal
        foreach (var member in TypeSymbol.GetMembers())
        {
            switch (member)
            {
                case IPropertySymbol propertySymbol when IsPublicOrInternal(propertySymbol):
                    members.Add(new GraphQLSourceTypeMember(propertySymbol));
                    break;
                case IFieldSymbol { IsImplicitlyDeclared: false } fieldSymbol when IsPublicOrInternal(fieldSymbol):
                    members.Add(new GraphQLSourceTypeMember(fieldSymbol));
                    break;
            }
        }

        return members;
    }

    private static bool IsPublicOrInternal(ISymbol symbol)
    {
        if (symbol.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
        {
            return false;
        }

        // For properties, check that at least the getter is public or internal
        // (we want to be able to read the property for GraphQL field resolution)
        if (symbol is IPropertySymbol propertySymbol)
        {
            var getterAccessibility = propertySymbol.GetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable;
            if (getterAccessibility is Accessibility.NotApplicable)
            {
                // No getter, can't read the property
                return false;
            }

            // Check if the getter is accessible (public or internal)
            return getterAccessibility is Accessibility.Public or Accessibility.Internal;
        }

        return true;
    }

    /// <summary>
    /// Gets a member by name.
    /// </summary>
    public GraphQLSourceTypeMember? GetMember(string name)
    {
        return Members.FirstOrDefault(m => m.Name == name);
    }

    /// <summary>
    /// Checks if the source type has a member with the specified name.
    /// </summary>
    public bool HasMember(string name)
    {
        return GetMember(name) != null;
    }
}
