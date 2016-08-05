using System;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator.Models
{
    /// <summary>
    ///    Field definition needed for a schema.
    /// </summary>
    public class FieldDefinition
    {
        /// <summary>
        ///     Type of response.
        /// </summary>
        public FieldInformation Field { get; }

        /// <summary>
        ///     Resolve function to get the object.
        /// </summary>
        public Func<ResolveFieldContext, object> Resolve { get; }

        public FieldDefinition(FieldInformation field, Func<ResolveFieldContext, object> resolve)
        {
            Field = field;
            Resolve = resolve;
        }
    }
}
