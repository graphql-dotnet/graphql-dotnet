using GraphQLParser;

namespace GraphQL.Types;

/// <summary>
/// Represents a placeholder for another GraphQL Output type, referenced by CLR type. Must be replaced with a
/// reference to the actual GraphQL type before using the reference.
/// </summary>
public sealed class GraphQLClrOutputTypeReference<[NotAGraphType] T> : IInterfaceGraphType, IObjectGraphType
{
    private GraphQLClrOutputTypeReference()
    {
    }

    Func<object, bool>? IObjectGraphType.IsTypeOf { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    bool IObjectGraphType.SkipTypeCheck { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    Interfaces IImplementInterfaces.Interfaces => throw new NotImplementedException();
    ResolvedInterfaces IImplementInterfaces.ResolvedInterfaces => throw new NotImplementedException();
    Func<object, IObjectGraphType?>? IAbstractGraphType.ResolveType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    PossibleTypes IAbstractGraphType.PossibleTypes => throw new NotImplementedException();
    IEnumerable<Type> IAbstractGraphType.Types { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    TypeFields IComplexGraphType.Fields => throw new NotImplementedException();
    bool IGraphType.IsPrivate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    IMetadataReader IMetadataWriter.MetadataReader => throw new NotImplementedException();
    Dictionary<string, object?> IProvideMetadata.Metadata => throw new NotImplementedException();
    string? IProvideDescription.Description { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    string? IProvideDeprecationReason.DeprecationReason { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    string INamedType.Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    FieldType IComplexGraphType.AddField(FieldType fieldType) => throw new NotImplementedException();
    void IAbstractGraphType.AddPossibleType(IObjectGraphType type) => throw new NotImplementedException();
    void IImplementInterfaces.AddResolvedInterface(IInterfaceGraphType graphType) => throw new NotImplementedException();
    FieldType? IComplexGraphType.GetField(ROM name) => throw new NotImplementedException();
    TType IProvideMetadata.GetMetadata<TType>(string key, TType defaultValue) => throw new NotImplementedException();
    TType IProvideMetadata.GetMetadata<TType>(string key, Func<TType> defaultValueFactory) => throw new NotImplementedException();
    bool IComplexGraphType.HasField(string name) => throw new NotImplementedException();
    bool IProvideMetadata.HasMetadata(string key) => throw new NotImplementedException();
    void IGraphType.Initialize(ISchema schema) => throw new NotImplementedException();
    void IAbstractGraphType.Type(Type type) => throw new NotImplementedException();
    void IAbstractGraphType.Type<TType>() => throw new NotImplementedException();
}
