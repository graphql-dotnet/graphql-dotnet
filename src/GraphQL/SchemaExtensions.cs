using System;
using System.Threading.Tasks;
using GraphQL.Introspection;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for schemas.
    /// </summary>
    public static class SchemaExtensions
    {
        /// <summary>
        /// Adds the specified visitor type to the schema. When initializing a schema, all
        /// registered visitors will be executed on each schema element when it is traversed.
        /// </summary>
        public static TSchema RegisterVisitor<TSchema, TVisitor>(this TSchema schema)
            where TSchema : ISchema
            where TVisitor : ISchemaNodeVisitor
        {
            schema.RegisterVisitor(typeof(TVisitor));
            return schema;
        }

        /// <summary>
        /// Adds the specified graph type to the schema.
        /// <br/><br/>
        /// Not typically required as schema initialization will scan the <see cref="ISchema.Query"/>,
        /// <see cref="ISchema.Mutation"/> and <see cref="ISchema.Subscription"/> graphs, creating
        /// instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        public static void RegisterType<T>(this ISchema schema)
            where T : IGraphType
        {
            schema.RegisterType(typeof(T));
        }

        /// <summary>
        /// Adds the specified graph types to the schema. Each type must implement <see cref="IGraphType"/>.
        /// <br/><br/>
        /// Not typically required as schema initialization will scan the <see cref="ISchema.Query"/>,
        /// <see cref="ISchema.Mutation"/> and <see cref="ISchema.Subscription"/> graphs, creating
        /// instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        public static TSchema RegisterTypes<TSchema>(this TSchema schema, params Type[] types)
            where TSchema : ISchema
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (var type in types)
            {
                schema.RegisterType(type);
            }

            return schema;
        }

        /// <summary>
        /// Adds the specified instances of <see cref="IGraphType"/>s to the schema.
        /// <br/><br/>
        /// Not typically required as schema initialization will scan the <see cref="ISchema.Query"/>,
        /// <see cref="ISchema.Mutation"/> and <see cref="ISchema.Subscription"/> graphs, creating
        /// instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        public static void RegisterTypes<TSchema>(this TSchema schema, params IGraphType[] types)
            where TSchema : ISchema
        {
            foreach (var type in types)
                schema.RegisterType(type);
        }

        /// <summary>
        /// Enables some experimental features that are not in the official specification, i.e. ability to expose
        /// user-defined meta-information via introspection. See https://github.com/graphql/graphql-spec/issues/300
        /// for more information.
        /// </summary>
        /// <param name="schema">The schema for which the features are enabled.</param>
        /// <param name="mode">Experimental features mode.</param>
        /// <returns>Reference to the provided <paramref name="schema"/>Experimental features mode.</returns>
        public static TSchema EnableExperimentalIntrospectionFeatures<TSchema>(this TSchema schema, ExperimentalIntrospectionFeaturesMode mode = ExperimentalIntrospectionFeaturesMode.ExecutionOnly)
            where TSchema : ISchema
        {
            if (schema.Initialized)
                throw new InvalidOperationException("Schema is already initialized");

            schema.Features.AppliedDirectives = true;
            if (mode == ExperimentalIntrospectionFeaturesMode.IntrospectionAndExecution)
                schema.Filter = new ExperimentalIntrospectionFeaturesSchemaFilter();

            return schema;
        }

        /// <summary>
        /// Executes a GraphQL request with the default <see cref="DocumentExecuter"/>, serializes the result using the specified <see cref="IDocumentWriter"/>, and returns the result
        /// </summary>
        /// <param name="schema">An instance of <see cref="ISchema"/> to use to execute the query</param>
        /// <param name="documentWriter">An instance of <see cref="IDocumentExecuter"/> to use to serialize the result</param>
        /// <param name="configure">A delegate which configures the execution options</param>
        public static async Task<string> ExecuteAsync(this ISchema schema, IDocumentWriter documentWriter, Action<ExecutionOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var executor = new DocumentExecuter();
            var result = await executor.ExecuteAsync(options =>
            {
                options.Schema = schema;
                configure(options);
            }).ConfigureAwait(false);

            return await documentWriter.WriteToStringAsync(result).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs the specified visitor on the specified schema.
        /// </summary>
        public static void Run(this ISchemaNodeVisitor visitor, ISchema schema) => schema.Run(visitor);

        /// <summary>
        /// Runs the specified visitor on the specified schema.
        /// </summary>
        public static void Run(this ISchema schema, ISchemaNodeVisitor visitor)
        {
            visitor.VisitSchema(schema);

            foreach (var directive in schema.Directives.List)
            {
                visitor.VisitDirective(directive, schema);

                if (directive.Arguments?.Count > 0)
                {
                    foreach (var argument in directive.Arguments.List)
                        visitor.VisitDirectiveArgumentDefinition(argument, schema);
                }
            }

            foreach (var item in schema.AllTypes.Dictionary)
            {
                switch (item.Value)
                {
                    case EnumerationGraphType e:
                        visitor.VisitEnum(e, schema);
                        foreach (var value in e.Values.List)
                            visitor.VisitEnumValue(value, schema);
                        break;

                    case ScalarGraphType scalar:
                        visitor.VisitScalar(scalar, schema);
                        break;

                    case UnionGraphType union:
                        visitor.VisitUnion(union, schema);
                        break;

                    case InterfaceGraphType iface:
                        visitor.VisitInterface(iface, schema);
                        break;

                    case IObjectGraphType output:
                        visitor.VisitObject(output, schema);
                        foreach (var field in output.Fields.List)
                        {
                            visitor.VisitFieldDefinition(field, schema);
                            if (field.Arguments?.Count > 0)
                            {
                                foreach (var argument in field.Arguments.List)
                                    visitor.VisitFieldArgumentDefinition(argument, schema);
                            }
                        }
                        break;

                    case IInputObjectGraphType input:
                        visitor.VisitInputObject(input, schema);
                        foreach (var field in input.Fields.List)
                            visitor.VisitInputFieldDefinition(field, schema);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// A way to use experimental features.
    /// </summary>
    public enum ExperimentalIntrospectionFeaturesMode
    {
        /// <summary>
        /// Allow experimental features only for client queries but not for standard introspection
        /// request. This means that the client, in response to a standard introspection request,
        /// receives a standard response without all the new fields and types. However, client CAN
        /// make requests to the server using the new fields and types. This mode is needed in order
        /// to bypass the problem of tools such as GraphQL Playground, Voyager, GraphiQL that require
        /// a standard response to an introspection request and refuse to work correctly if receive
        /// unknown fields or types in the response.
        /// </summary>
        ExecutionOnly,

        /// <summary>
        /// Allow experimental features for both standard introspection query and client queries.
        /// This means that the client, in response to a standard introspection request, receives
        /// a response augmented with the new fields and types. Client can make requests to the
        /// server using the new fields and types.
        /// </summary>
        IntrospectionAndExecution
    }
}
