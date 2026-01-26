using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Types;

/// <summary>
/// Represents a placeholder for another GraphQL Input type, referenced by CLR type. Must be replaced with a
/// reference to the actual GraphQL type before using the reference.
/// </summary>
public sealed class GraphQLClrInputTypeReference<[NotAGraphType] T> : IInputObjectGraphType
    where T : notnull
{
    private GraphQLClrInputTypeReference()
    {
    }

    bool IInputObjectGraphType.IsOneOf { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    TypeFields IComplexGraphType.Fields => throw new NotImplementedException();
    bool IGraphType.IsPrivate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    IMetadataReader IMetadataWriter.MetadataReader => throw new NotImplementedException();
    Dictionary<string, object?> IProvideMetadata.Metadata => throw new NotImplementedException();
    string? IProvideDescription.Description { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    string? IProvideDeprecationReason.DeprecationReason { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    string INamedType.Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    FieldType IComplexGraphType.AddField(FieldType fieldType) => throw new NotImplementedException();
    FieldType? IComplexGraphType.GetField(ROM name) => throw new NotImplementedException();
    TType IProvideMetadata.GetMetadata<TType>(string key, TType defaultValue) => throw new NotImplementedException();
    TType IProvideMetadata.GetMetadata<TType>(string key, Func<TType> defaultValueFactory) => throw new NotImplementedException();
    bool IComplexGraphType.HasField(string name) => throw new NotImplementedException();
    bool IProvideMetadata.HasMetadata(string key) => throw new NotImplementedException();
    void IGraphType.Initialize(ISchema schema) => throw new NotImplementedException();
    bool IInputObjectGraphType.IsValidDefault(object value) => throw new NotImplementedException();
    object IInputObjectGraphType.ParseDictionary(IDictionary<string, object?> value, IValueConverter valueConverter) => throw new NotImplementedException();
    GraphQLValue IInputObjectGraphType.ToAST(object value) => throw new NotImplementedException();
}
