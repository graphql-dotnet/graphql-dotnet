using System.Diagnostics;
using GraphQLParser;

namespace GraphQL.Types;

/// <summary>
/// Represents a placeholder for another GraphQL type, referenced by name. Must be replaced with a
/// reference to the actual GraphQL type before using the reference.
/// </summary>
[DebuggerDisplay("ref {TypeName,nq}")]
public sealed class GraphQLTypeReference : IInterfaceGraphType, IObjectGraphType
{
    /// <summary>
    /// Initializes a new instance for the specified graph type name.
    /// </summary>
    public GraphQLTypeReference(string typeName)
    {
        TypeName = typeName;
    }

    /// <summary>
    /// Returns the GraphQL type name that this reference is a placeholder for.
    /// </summary>
    public string TypeName { get; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is GraphQLTypeReference other
            ? TypeName == other.TypeName
            : base.Equals(obj);
    }

    /// <inheritdoc/>
    public override string ToString() => TypeName;

    /// <inheritdoc/>
    public override int GetHashCode() => TypeName?.GetHashCode() ?? 0;

    void IAbstractGraphType.AddPossibleType(IObjectGraphType type) => throw Invalid();
    void IAbstractGraphType.Type(Type type) => throw Invalid();
    void IAbstractGraphType.Type<TType>() => throw Invalid();
    FieldType IComplexGraphType.AddField(FieldType fieldType) => throw Invalid();
    bool IComplexGraphType.HasField(string name) => throw Invalid();
    FieldType? IComplexGraphType.GetField(ROM name) => throw Invalid();
    void IImplementInterfaces.AddResolvedInterface(IInterfaceGraphType graphType) => throw Invalid();
    void IGraphType.Initialize(ISchema schema) => throw Invalid();
    TType IProvideMetadata.GetMetadata<TType>(string key, TType defaultValue) => throw Invalid();
    TType IProvideMetadata.GetMetadata<TType>(string key, Func<TType> defaultValueFactory) => throw Invalid();
    bool IProvideMetadata.HasMetadata(string key) => throw Invalid();
    Func<object, IObjectGraphType?>? IAbstractGraphType.ResolveType { get => throw Invalid(); set => throw Invalid(); }
    PossibleTypes IAbstractGraphType.PossibleTypes => throw Invalid();
    IEnumerable<Type> IAbstractGraphType.Types { get => throw Invalid(); set => throw Invalid(); }
    TypeFields IComplexGraphType.Fields => throw Invalid();
    Interfaces IImplementInterfaces.Interfaces => throw Invalid();
    ResolvedInterfaces IImplementInterfaces.ResolvedInterfaces => throw Invalid();
    bool IGraphType.IsPrivate { get => throw Invalid(); set => throw Invalid(); }
    IMetadataReader IMetadataWriter.MetadataReader => throw Invalid();
    Dictionary<string, object?> IProvideMetadata.Metadata => throw Invalid();
    string? IProvideDescription.Description { get => throw Invalid(); set => throw Invalid(); }
    string? IProvideDeprecationReason.DeprecationReason { get => throw Invalid(); set => throw Invalid(); }
    string INamedType.Name { get => "__GraphQLTypeReference"; set => throw Invalid(); }
    Func<object, bool>? IObjectGraphType.IsTypeOf { get => throw Invalid(); set => throw Invalid(); }
    bool IObjectGraphType.SkipTypeCheck { get => throw Invalid(); set => throw Invalid(); }

    private InvalidOperationException Invalid() => new($"This is just a reference to '{TypeName}'. Resolve the real type first.");
}
