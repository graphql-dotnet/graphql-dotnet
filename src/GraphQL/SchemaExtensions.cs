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
        /// Enables some experimental features that are not in the official specification, i.e. ability to expose
        /// user-defined meta-information via introspection. See https://github.com/graphql/graphql-spec/issues/300
        /// for more information.
        /// </summary>
        /// <param name="schema">The schema for which the features are enabled.</param>
        /// <param name="mode">Experimental features mode.</param>
        /// <returns>Reference to the provided <paramref name="schema"/>Experimental features mode.</returns>
        public static TSchema EnableExperimentalIntrospectionFeatures<TSchema>(this TSchema schema, ExperimentalIntrospectionFeaturesMode mode = ExperimentalIntrospectionFeaturesMode.ExecutionOnly)
            where TSchema : Schema
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
        public static void Run(this Schema schema, ISchemaNodeVisitor visitor)
        {
            visitor.VisitSchema(schema);

            foreach (var directive in schema.Directives.List)
            {
                visitor.VisitDirective(directive);

                if (directive.Arguments?.Count > 0)
                {
                    foreach (var argument in directive.Arguments.List)
                        visitor.VisitDirectiveArgumentDefinition(argument);
                }
            }

            foreach (var item in schema.AllTypes.Dictionary)
            {
                switch (item.Value)
                {
                    case EnumerationGraphType e:
                        visitor.VisitEnum(e);
                        foreach (var value in e.Values.List)
                            visitor.VisitEnumValue(value);
                        break;

                    case ScalarGraphType scalar:
                        visitor.VisitScalar(scalar);
                        break;

                    case UnionGraphType union:
                        visitor.VisitUnion(union);
                        break;

                    case InterfaceGraphType iface:
                        visitor.VisitInterface(iface);
                        break;

                    case IObjectGraphType output:
                        visitor.VisitObject(output);
                        foreach (var field in output.Fields.List)
                        {
                            visitor.VisitFieldDefinition(field);
                            if (field.Arguments?.Count > 0)
                            {
                                foreach (var argument in field.Arguments.List)
                                    visitor.VisitFieldArgumentDefinition(argument);
                            }
                        }
                        break;

                    case IInputObjectGraphType input:
                        visitor.VisitInputObject(input);
                        foreach (var field in input.Fields.List)
                            visitor.VisitInputFieldDefinition(field);
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
