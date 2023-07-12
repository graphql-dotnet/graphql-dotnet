using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.Extensions;

/// <summary>
/// A schema builder for GraphQL federation.
/// </summary>
public class FederatedSchemaBuilder : SchemaBuilder
{
    // https://www.apollographql.com/docs/federation/federation-spec/
    private const string FEDERATED_SDL = @"
directive @key(fields: String!, resolvable: Boolean) repeatable on OBJECT | INTERFACE
directive @provides(fields: String!) on FIELD_DEFINITION
directive @requires(fields: String!) on FIELD_DEFINITION
directive @external on FIELD_DEFINITION
directive @shareable on FIELD_DEFINITION | OBJECT
directive @override(from: String!) on FIELD_DEFINITION
directive @inaccessible on FIELD_DEFINITION | OBJECT
";

    /// <inheritdoc/>
    public override Schema Build(string typeDefinitions) =>
        base.Build($"{FEDERATED_SDL}{Environment.NewLine}{typeDefinitions}");
}
