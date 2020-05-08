using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    public static class MySchemaBuilderExtensions
    {
        /// <summary>
        /// Internal helper to build GraphQLDirective instance which is then used by FederationSchemaPrinter while printing federated schema sdl
        /// Directives are stored in fields metadata under key "__AST_MetaField__" and does not used anywhere else
        /// Additional properties like Location are added to avoid null ref exceptions only
        /// </summary>
        /// <param name="name">name of the directive, e.g. key, extend, provides, requires</param>
        /// <param name="value">value for fields argument, e.g. @key(fields: "id")</param>
        /// <param name="kind">for field directives - ASTNodeKind.StringValue, for type directives - ASTNodeKind.Argument</param>
        /// <returns></returns>
        private static GraphQLDirective BuildGraphQLDirective(string name, string value = null, ASTNodeKind kind = ASTNodeKind.StringValue) => new GraphQLDirective
        {
            Name = new GraphQLName
            {
                Value = name,
                Location = new GraphQLLocation()
            },
            Arguments = string.IsNullOrEmpty(value) ? new List<GraphQLArgument>() : new List<GraphQLArgument>() {
                        new GraphQLArgument
                        {
                            Name = new GraphQLName {
                                Value = "fields",
                                Location = new GraphQLLocation()
                            },
                            Value = new GraphQLScalarValue(kind) {
                                Value = value,
                                Location = new GraphQLLocation()
                            },
                            Location = new GraphQLLocation()
                        }
                    },
            Location = new GraphQLLocation()
        };

        /// <summary>
        /// The same as above
        /// </summary>
        /// <returns></returns>
        private static GraphQLObjectTypeDefinition BuildGraphQLObjectTypeDefinition() => new GraphQLObjectTypeDefinition
        {
            Directives = new List<GraphQLDirective>(),
            Location = new GraphQLLocation(),
            Fields = new List<GraphQLFieldDefinition>()
        };

        private static void AddDirective(GraphQLObjectTypeDefinition definition, GraphQLDirective directive) => ((List<GraphQLDirective>)definition.Directives).Add(directive);

        /// <summary>
        /// Public helper which will allow us to set "__EXTENSION_AST_MetaField__" on a type which then will be used by FederatedSchemaPrinter
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void BuildExtensionAstMeta(this IProvideMetadata type, string name, string value = null)
        {
            var definition = BuildGraphQLObjectTypeDefinition();
            var directive = BuildGraphQLDirective(name, value, ASTNodeKind.Argument);
            AddDirective(definition, directive);
            // type.AddExtensionAstType(definition);
            type.Metadata["__EXTENSION_AST_MetaField__"] = new List<ASTNode> { definition };
        }

        /// <summary>
        /// Public helper which will allow us to set "__AST_MetaField__" on a field which then will be used by FederatedSchemaPrinter
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void BuildAstMeta(this IProvideMetadata type, string name, string value = null)
        {
            // var definition = type.GetAstType<GraphQLObjectTypeDefinition>() ?? BuildGraphQLObjectTypeDefinition();
            var definition = (GraphQLObjectTypeDefinition)type.GetMetadata<ASTNode>("__AST_MetaField__", () => BuildGraphQLObjectTypeDefinition());
            var directive = BuildGraphQLDirective(name, value);
            AddDirective(definition, directive);
            //type.SetAstType(definition);
            type.Metadata["__AST_MetaField__"] = definition;
        }
    }

    /// <summary>
    /// FieldBuillderExtensions, provide shortcuts for filling field metadata with expected objects, e.g. instead of:
    /// Field().Name("foo").FieldType.Metadata["__AST_MetaField__"] = new GraphQLObjectTypeDefinition() {...};
    /// we will have:
    /// Field().Name("foo").Requires("bar");
    /// </summary>
    public static class FederatedFieldBuilderExtensions
    {
        private static FieldBuilder<TSourceType, TReturnType> BuildAstMeta<TSourceType, TReturnType>(FieldBuilder<TSourceType, TReturnType> fieldBuilder, string name, string value = null)
        {
            fieldBuilder.FieldType.BuildAstMeta(name, value);
            return fieldBuilder;
        }

        public static FieldBuilder<TSourceType, TReturnType> Key<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder, string fields) => BuildAstMeta(fieldBuilder, "key", fields);
        public static FieldBuilder<TSourceType, TReturnType> Requires<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder, string fields) => BuildAstMeta(fieldBuilder, "requires", fields);
        public static FieldBuilder<TSourceType, TReturnType> Provides<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder, string fields) => BuildAstMeta(fieldBuilder, "provides", fields);
        public static FieldBuilder<TSourceType, TReturnType> External<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder) => BuildAstMeta(fieldBuilder, "external");
    }

    /// <summary>
    /// Can be replaced with ObjectGraphType extensions, the same as above - idea to allow us to define type directives, e.g.:
    /// public class AcmeType: ObjectGraphType {
    ///   public AcmeType() {
    ///     // Metadata["__AST_MetaField__"] = new GraphQLObjectTypeDefinition() {...};
    ///     Key("id");
    ///   }
    /// }
    ///
    /// TODO:
    /// - there is only two cases: `type Account @key(fields:"id")` and `extend type Account @key(fields:"id")`
    /// - will be nice to have to enforce declaration one of above
    /// - will be nice to have to enforce resolve reference declaration
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    public class FederatedObjectGraphType<TSourceType> : ObjectGraphType<TSourceType>
    {
        public void Key(string fields) => this.BuildAstMeta("key", fields);

        public void ExtendByKey(string fields)
        {
            this.BuildExtensionAstMeta("key", fields);
            Key(fields);
        }

        // Resolve reference is from schema first aproach
        public void ResolveReferenceAsync(Func<FederatedResolveContext, Task<TSourceType>> resolver) => ResolveReferenceAsync(new FuncFederatedResolver<TSourceType>(resolver));

        public void ResolveReferenceAsync(IFederatedResolver resolver)
        {
            // Metadata[FederatedSchemaBuilder.RESOLVER_METADATA_FIELD] = resolver;
            Metadata["__FedResolver__"] = resolver;
        }
    }

    // TODO: move me into separate project
    //public static class FederatedGraphQLBuilderExtensions
    //{
    //    public static IGraphQLBuilder AddFederation(this IGraphQLBuilder builder)
    //    {
    //        builder.Services.AddSingleton<AnyScalarGraphType>();
    //        builder.Services.AddSingleton<ServiceGraphType>();

    //        return builder;
    //    }
    //}


    /// <summary>
    /// Small extensions just to hide registration of required types, can be extension instead
    /// </summary>
    public class FederatedSchemaProposal : Schema
    {
        public FederatedSchemaProposal() : this(new DefaultServiceProvider()) { }
        public FederatedSchemaProposal(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterValueConverter(new AnyValueConverter());
            RegisterType<AnyScalarGraphType>();
            RegisterType<ServiceGraphType>();
            RegisterType<EntityType>();
        }
    }

    /// <summary>
    /// TODO:
    /// - implicit vs explicit
    /// </summary>
    public class EntityType : UnionGraphType {
        public EntityType()
        {
            Name = "_Entity";
        }
    }

    /// <summary>
    /// Small helper which does add Query._service and Query._entities
    /// Resolvers are the same as in FederatedSchemaBuilder so can be reused
    ///
    /// TODO:
    /// - extract resolvers into base class
    /// - reuse resolvers in both FederatedQuery and FederatedSchemaBuilder
    /// </summary>
    public class FederatedQuery : ObjectGraphType
    {
        public FederatedQuery()
        {
            Field<NonNullGraphType<ServiceGraphType>>().Name("_service").Resolve(context => new { });

            Field<NonNullGraphType<ListGraphType<EntityType>>>()
                .Name("_entities")
                .Argument<NonNullGraphType<ListGraphType<NonNullGraphType<AnyScalarGraphType>>>>("representations")
                .ResolveAsync(context =>
                {
                    var representations = context.GetArgument<List<Dictionary<string, object>>>("representations");
                    var results = new List<Task<object>>();

                    foreach (var representation in representations)
                    {
                        var typeName = representation["__typename"].ToString();
                        var type = context.Schema.FindType(typeName);

                        if (type != null)
                        {
                            // execute resolver
                            var resolver = type.GetMetadata<IFederatedResolver>("__FedResolver__");
                            if (resolver != null)
                            {
                                var resolveContext = new FederatedResolveContext
                                {
                                    Arguments = representation,
                                    ParentFieldContext = (ResolveFieldContext)context
                                };
                                var result = resolver.Resolve(resolveContext);
                                results.Add(result);
                            }
                            else
                            {
                                results.Add(Task.FromResult((object)representation));
                            }
                        }
                        else
                        {
                            // otherwise return the representation
                            results.Add(Task.FromResult((object)representation));
                        }
                    }

                    var tasks = Task.WhenAll(results).ContinueWith(results => (object)results.Result);
                    tasks.ConfigureAwait(false);
                    return tasks;
                });
        }
    }
}
