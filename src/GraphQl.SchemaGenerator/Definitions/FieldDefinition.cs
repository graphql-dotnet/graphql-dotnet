using System;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator.Definitions
{
    /// <summary>
    ///    Field definition needed for a schema.
    /// </summary>
    public class FieldDefinition
    {
        /// <summary>
        ///     Type of response.
        /// </summary>
        public Type Response { get; }

        /// <summary>
        ///     Name of field.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Query arguments.
        /// </summary>
        public QueryArguments Arguments { get; }

        /// <summary>
        ///     Resolve function to get the object.
        /// </summary>
        public Func<ResolveFieldContext, object> Resolve { get; }

        public bool IsMutation { get; }

        public FieldDefinition(Type response, string name, QueryArguments arguments, bool isMutation, Func<ResolveFieldContext, object> resolve)
        {
            Response = response;
            Name = name;
            Arguments = arguments;
            IsMutation = isMutation;
            Resolve = resolve;
        }
    }
}
