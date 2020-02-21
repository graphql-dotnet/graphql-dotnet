using GraphQL.Conversion;
using GraphQL.Introspection;
using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    /// <summary>
    /// The schema for the GraphQL request. Contains references to the 'query', 'mutation', and 'subscription' base graph types.<br/>
    /// <br/>
    /// Also allows for adding custom directives, additional graph types, and custom value converters.<br/>
    /// <br/>
    /// <see cref="Schema"/> only requires the <see cref="Schema.Query">Query</see> property to be set; although commonly the <see cref="Schema.Mutation">Mutation</see> and/or <see cref="Schema.Subscription">Subscription</see> properties are also set.
    /// </summary>
    public interface ISchema
    {
        /// <summary>
        /// Returns true once the schema has been initialized
        /// </summary>
        bool Initialized { get; }

        /// <summary>
        /// Initializes the schema. Called by <see cref="IDocumentExecuter"/> before validating or executing the request.<br/>
        /// <br/>
        /// Note that middleware cannot be applied once the schema has been initialized. See <see cref="ExecutionOptions.FieldMiddleware"/>.
        /// </summary>
        void Initialize();

        /// <summary>
        /// The <see cref="IFieldNameConverter"/> used by the schema. This is set by <see cref="IDocumentExecuter"/> to the converter passed to it within <see cref="ExecutionOptions.FieldNameConverter"/>.
        /// </summary>
        IFieldNameConverter FieldNameConverter { get; set; }

        /// <summary>
        /// The 'query' base graph type; required
        /// </summary>
        IObjectGraphType Query { get; set; }

        /// <summary>
        /// The 'mutation' base graph type; optional
        /// </summary>
        IObjectGraphType Mutation { get; set; }

        /// <summary>
        /// The 'subscription' base graph type; optional
        /// </summary>
        IObjectGraphType Subscription { get; set; }

        /// <summary>
        /// Returns a list of directives supported by the schema.<br/>
        /// <br/>
        /// Directives are used by the GraphQL runtime as a way of modifying execution
        /// behavior. Type system creators do not usually create them directly.<br/>
        /// <br/>
        /// <see cref="Schema"/> intializes the list to include <see cref="DirectiveGraphType.Include"/>, <see cref="DirectiveGraphType.Skip"/> and <see cref="DirectiveGraphType.Deprecated"/> by default.
        /// </summary>
        IEnumerable<DirectiveGraphType> Directives { get; set; }

        /// <summary>
        /// Returns a list of all the graph types utilized by this schema
        /// </summary>
        IEnumerable<IGraphType> AllTypes { get; }

        /// <summary>
        /// Returns a <see cref="IGraphType"/> for a given name
        /// </summary>
        IGraphType FindType(string name);

        /// <summary>
        /// Returns a <see cref="DirectiveGraphType"/> for a given name
        /// </summary>
        DirectiveGraphType FindDirective(string name);

        /// <summary>
        /// A list of additional graph types manually added to the schema by RegisterType call.
        /// </summary>
        IEnumerable<Type> AdditionalTypes { get; }

        /// <summary>
        /// Add a specific instance of an <see cref="IGraphType"/> to the schema.<br/>
        /// <br/>
        /// Not typically required as schema initialization will scan the <see cref="Query"/>, <see cref="Mutation"/> and <see cref="Subscription"/> graphs,
        /// creating instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        void RegisterType(IGraphType type);

        /// <summary>
        /// Add specific instances of <see cref="IGraphType"/>s to the schema.<br/>
        /// <br/>
        /// Not typically required as schema initialization will scan the <see cref="Query"/>, <see cref="Mutation"/> and <see cref="Subscription"/> graphs,
        /// creating instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        void RegisterTypes(params IGraphType[] types);

        /// <summary>
        /// Add specific graph types to the schema. Each type must implement <see cref="IGraphType"/>.<br/>
        /// <br/>
        /// Not typically required as schema initialization will scan the <see cref="Query"/>, <see cref="Mutation"/> and <see cref="Subscription"/> graphs,
        /// creating instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        void RegisterTypes(params Type[] types);

        /// <summary>
        /// Add a specific graph type to the schema.<br/>
        /// <br/>
        /// Not typically required as schema initialization will scan the <see cref="Query"/>, <see cref="Mutation"/> and <see cref="Subscription"/> graphs,
        /// creating instances of <see cref="IGraphType"/>s referenced therein as necessary.
        /// </summary>
        void RegisterType<T>() where T : IGraphType;

        /// <summary>
        /// Add a specific directive to the schema.<br/>
        /// <br/>
        /// Directives are used by the GraphQL runtime as a way of modifying execution
        /// behavior. Type system creators do not usually create them directly.
        /// </summary>
        void RegisterDirective(DirectiveGraphType directive);

        /// <summary>
        /// Add specific directives to the schema.<br/>
        /// <br/>
        /// Directives are used by the GraphQL runtime as a way of modifying execution
        /// behavior. Type system creators do not usually create them directly.
        /// </summary>
        void RegisterDirectives(params DirectiveGraphType[] directives);

        /// <summary>
        /// Register a custom value converter to the schema.
        /// </summary>
        void RegisterValueConverter(IAstFromValueConverter converter);

        /// <summary>
        /// Search the schema for a <see cref="IAstFromValueConverter"/> that matches the provided object and graph type, and return the converter.
        /// </summary>
        IAstFromValueConverter FindValueConverter(object value, IGraphType type);

        /// <summary>
        /// Provides the ability to filter the schema upon introspection to hide types. This is set by <see cref="IDocumentExecuter"/>
        /// to the filter passed to it within <see cref="ExecutionOptions.SchemaFilter"/>. By default, no types are hidden.
        /// Note that this filter in fact does not prohibit the execution of queries that contain hidden types. To limit
        /// access to the particular fields, you should use some authorization logic.
        /// </summary>
        ISchemaFilter Filter { get; set; }
    }
}
