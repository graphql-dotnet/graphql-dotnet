#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
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
            schema.RegisterType<ServiceGraphType>();
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

            var entities = new FieldType
            {
                Name = "_entities",
                Arguments = new QueryArguments(new QueryArgument(representationsType) { Name = "representations" }),
                ResolvedType = new NonNullGraphType(new ListGraphType(new GraphQLTypeReference("_Entity"))),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    AddTypeNameToSelection(context.FieldAst, context.Document);

                    var reps = context.GetArgument<List<Dictionary<string, object>>>("representations");

                    var results = new List<object?>();

                    foreach (var rep in reps!)
                    {
                        var typeName = rep!["__typename"].ToString();
                        var type = context.Schema.AllTypes[typeName!];
                        if (type != null)
                        {
                            // execute resolver
                            var resolver = type.GetMetadata<IFederatedResolver>(RESOLVER_METADATA_FIELD);
                            if (resolver != null)
                            {
                                var resolveContext = new FederatedResolveContext
                                {
                                    Arguments = rep!,
                                    ParentFieldContext = context
                                };
                                var result = await resolver.Resolve(resolveContext).ConfigureAwait(false);
                                results.Add(result);
                            }
                            else
                            {
                                results.Add(rep);
                            }
                        }
                        else
                        {
                            // otherwise return the representation
                            results.Add(rep);
                        }
                    }

                    return results;
                })
            };
            query.AddField(entities);
        }

        private void AddTypeNameToSelection(GraphQLField field, GraphQLDocument document)
        {
            if (FindSelectionToAmend(field.SelectionSet!, document, out var setToAlter))
            {
                setToAlter!.Selections.Insert(0, new GraphQLField { Name = new GraphQLName("__typename") });
            }
        }

        private bool FindSelectionToAmend(GraphQLSelectionSet selectionSet, GraphQLDocument document, out GraphQLSelectionSet? setToAlter)
        {
            foreach (var selection in selectionSet.Selections)
            {
                if (selection is GraphQLField childField && childField.Name == "__typename")
                {
                    setToAlter = null;
                    return false;
                }

                if (selection is GraphQLInlineFragment frag)
                {
                    return FindSelectionToAmend(frag.SelectionSet, document, out setToAlter);
                }

                if (selection is GraphQLFragmentSpread spread)
                {
                    var def = document.FindFragmentDefinition(spread.FragmentName.Name)!;
                    return FindSelectionToAmend(def.SelectionSet, document, out setToAlter);
                }
            }
            setToAlter = selectionSet;
            return true;
        }

        private EntityGraphType BuildEntityGraphType()
        {
            var entity = new EntityGraphType();

            //TODO: deal with 'x as IObjectGraphType', @key may be places on object OR interface
            var entities = _types.Values.Where(IsEntity).Select(x => x as IObjectGraphType).ToList();
            foreach (var e in entities)
            {
                entity.AddPossibleType(e!);
            }

            entity.ResolveType = x =>
            {
                if (x is Dictionary<string, object> dict && dict.TryGetValue("__typename", out object? typeName))
                {
                    return new GraphQLTypeReference(typeName!.ToString()!);
                }

                // TODO: Provide another way to give graph type name, such as an attribute
                return new GraphQLTypeReference(x.GetType().Name);
            };

            return entity;
        }

        private bool IsEntity(IGraphType type)
            => (type is IObjectGraphType || type is IInterfaceGraphType) && type.FindAppliedDirective("key") != null;
    }
}
