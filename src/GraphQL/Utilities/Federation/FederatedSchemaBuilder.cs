using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;
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

        public override ISchema Build(string typeDefinitions)
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
            schema.RegisterValueConverter(new AnyValueConverter());
        }

        private void AddRootEntityFields(ISchema schema)
        {
            var query = schema.Query;

            if (query == null)
            {
                schema.Query = query = new ObjectGraphType { Name = "Query" };
            }

            query.Field("_service", new NonNullGraphType(new GraphQLTypeReference("_Service")), resolve: context => new { });

            var representationsType = new NonNullGraphType(new ListGraphType(new NonNullGraphType(new GraphQLTypeReference("_Any"))));
            query.FieldAsync(
                "_entities",
                new NonNullGraphType(new ListGraphType(new GraphQLTypeReference("_Entity"))),
                arguments: new QueryArguments(new QueryArgument(representationsType) { Name = "representations" }),
                resolve: async context =>
                {
                    AddTypeNameToSelection(context.FieldAst, context.Document);

                    var reps = context.GetArgument<List<Dictionary<string, object>>>("representations");

                    var results = new List<object>();

                    foreach (var rep in reps)
                    {
                        var typeName = rep["__typename"].ToString();
                        var type = context.Schema.FindType(typeName);
                        if (type != null)
                        {
                            // execute resolver
                            var resolver = type.GetMetadata<IFederatedResolver>(RESOLVER_METADATA_FIELD);
                            if (resolver != null)
                            {
                                var resolveContext = new FederatedResolveContext
                                {
                                    Arguments = rep,
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
                });
        }

        private void AddTypeNameToSelection(Field field, Document document)
        {
            if (FindSelectionToAmend(field.SelectionSet, document, out var setToAlter))
            {
                setToAlter.Prepend(new Field(null, new NameNode("__typename")));
            }
        }

        private bool FindSelectionToAmend(SelectionSet selectionSet, Document document, out SelectionSet setToAlter)
        {
            foreach (var selection in selectionSet.Selections)
            {
                if (selection is Field childField && childField.Name == "__typename")
                {
                    setToAlter = null;
                    return false;
                }

                if (selection is InlineFragment frag)
                {
                    return FindSelectionToAmend(frag.SelectionSet, document, out setToAlter);
                }

                if (selection is FragmentSpread spread)
                {
                    var def = document.Fragments.FindDefinition(spread.Name);
                    return FindSelectionToAmend(def.SelectionSet, document, out setToAlter);
                }
            }
            setToAlter = selectionSet;
            return true;
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
                union.AddPossibleType(e);
            }

            union.ResolveType = x =>
            {
                if (x is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue("__typename", out object typeName))
                    {
                        return new GraphQLTypeReference(typeName.ToString());
                    }
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

            var directive = type.GetExtensionDirectives<ASTNode>().Directive("key");
            if (directive != null)
                return true;

            var ast = type.GetAstType<IHasDirectivesNode>();
            if (ast == null)
                return false;

            var keyDir = ast.Directives.Directive("key");
            return keyDir != null;
        }
    }
}
