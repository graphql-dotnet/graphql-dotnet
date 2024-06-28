#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Federation;
using GraphQL.Federation.Resolvers;
using GraphQL.Federation.Types;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation;

[Obsolete("Please use SchemaBuilder with graphQlBuilder.AddFederation() instead. This class will be removed in v9.")]
public class FederatedSchemaBuilder : SchemaBuilder
{
    internal const string RESOLVER_METADATA_FIELD = "__FedResolver__";

    private const string FEDERATED_SDL = @"
            scalar _Any
            # scalar _FieldSet

            # a union of all types that use the @key directive
            # union _Entity

            #type _Service {
            #    sdl: String
            #}

            #extend type Query {
            #    _entities(representations: [_Any!]!): [_Entity]!
            #    _service: _Service!
            #}

            directive @external on FIELD_DEFINITION
            directive @requires(fields: String!) on FIELD_DEFINITION
            directive @provides(fields: String!) on FIELD_DEFINITION
            directive @key(fields: String!) on OBJECT | INTERFACE

            # this is an optional directive
            directive @extends on OBJECT | INTERFACE
        ";

    public override Schema Build(string typeDefinitions)
    {
        var schema = base.Build($"{FEDERATED_SDL}{Environment.NewLine}{typeDefinitions}");
        schema.RegisterType(BuildEntityGraphType());
        AddRootEntityFields(schema);
        return schema;
    }

    protected override void PreConfigure(Schema schema)
    {
        schema.RegisterType<AnyScalarGraphType>();
        schema.RegisterType(new ServiceGraphType(new FederationPrintOptions
        {
            IncludeFederationTypes = false,
            IncludeImportedDefinitions = false
        })); // skip federation types for federation v1 support
    }

    private void AddRootEntityFields(ISchema schema)
    {
        var query = schema.Query;

        if (query == null)
        {
            schema.Query = query = new ObjectGraphType { Name = "Query" };
        }

        var service = new FieldType
        {
            Name = "_service",
            ResolvedType = new NonNullGraphType(new GraphQLTypeReference("_Service")),
            Resolver = new FuncFieldResolver<object>(_ => new { })
        };
        query.AddField(service);

        var representationsType = new NonNullGraphType(new ListGraphType(new NonNullGraphType(new GraphQLTypeReference("_Any"))));
        var representationArgument = new QueryArgument(representationsType)
        {
            Name = FederationHelper.REPRESENTATIONS_ARGUMENT,
            Parser = (value) => EntityResolver.Instance.ConvertRepresentations(schema, (System.Collections.IList)value)
        };

        var entities = new FieldType
        {
            Name = "_entities",
            Arguments = new QueryArguments(representationArgument),
            ResolvedType = new NonNullGraphType(new ListGraphType(new GraphQLTypeReference("_Entity"))),
            Resolver = EntityResolver.Instance,
        };
        query.AddField(entities);
    }

    private UnionGraphType BuildEntityGraphType()
    {
        var union = new UnionGraphType
        {
            Name = "_Entity",
            Description = "A union of all types that use the @key directive"
        };

        var entities = _types.Values.Where(IsEntity).Select(x => x as IObjectGraphType).ToList();
        foreach (var e in entities)
        {
            union.AddPossibleType(e!);
        }

        union.ResolveType = x =>
        {
            if (x is Dictionary<string, object> dict && dict.TryGetValue("__typename", out object? typeName))
            {
                return new GraphQLTypeReference(typeName!.ToString()!);
            }

            // TODO: Provide another way to give graph type name, such as an attribute
            return new GraphQLTypeReference(x.GetType().Name);
        };

        return union;
    }

    private bool IsEntity(IGraphType type)
    {
        if (type.IsInputObjectType())
        {
            return false;
        }

        var directive = Directive(type.GetExtensionDirectives<ASTNode>(), "key");
        if (directive != null)
            return true;

        var ast = type.GetAstType<IHasDirectivesNode>();
        if (ast == null)
            return false;

        var keyDir = Directive(ast.Directives!, "key");
        return keyDir != null;
    }

    private static GraphQLDirective? Directive(IEnumerable<GraphQLDirective> directives, string name) //TODO: remove?
    {
        return directives?.FirstOrDefault(x => x.Name == name);
    }
}
