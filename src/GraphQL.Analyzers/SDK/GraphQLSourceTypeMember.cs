using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents a field or property member of a source type.
/// </summary>
public sealed class GraphQLSourceTypeMember
{
    internal GraphQLSourceTypeMember(IPropertySymbol propertySymbol)
    {
        Symbol = propertySymbol;
        Name = propertySymbol.Name;
        IsReadable = propertySymbol.GetMethod != null;
        IsWritable = propertySymbol.SetMethod != null;
        Accessibility = propertySymbol.DeclaredAccessibility;
        IsProperty = true;
        IsField = false;
    }

    internal GraphQLSourceTypeMember(IFieldSymbol fieldSymbol)
    {
        Symbol = fieldSymbol;
        Name = fieldSymbol.Name;
        IsReadable = true;
        IsWritable = !fieldSymbol.IsReadOnly;
        Accessibility = fieldSymbol.DeclaredAccessibility;
        IsProperty = false;
        IsField = true;
    }

    /// <summary>
    /// Gets the underlying symbol (IPropertySymbol or IFieldSymbol).
    /// </summary>
    public ISymbol Symbol { get; }

    /// <summary>
    /// Gets the name of the member.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets whether the member is readable (can be accessed/get).
    /// </summary>
    public bool IsReadable { get; }

    /// <summary>
    /// Gets whether the member is writable (can be set).
    /// </summary>
    public bool IsWritable { get; }

    /// <summary>
    /// Gets the accessibility level of the member (public, private, protected, etc.).
    /// </summary>
    public Accessibility Accessibility { get; }

    /// <summary>
    /// Gets whether this member is a property.
    /// </summary>
    public bool IsProperty { get; }

    /// <summary>
    /// Gets whether this member is a field.
    /// </summary>
    public bool IsField { get; }

    /// <summary>
    /// Gets the type symbol of the member.
    /// </summary>
    public ITypeSymbol? Type => Symbol switch
    {
        IPropertySymbol property => property.Type,
        IFieldSymbol fldSymbol => fldSymbol.Type,
        _ => null
    };
}
