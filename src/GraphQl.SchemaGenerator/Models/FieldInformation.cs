using System;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator.Models
{
    /// <summary>
    ///    Field definition needed for a schema.
    /// </summary>
    public class FieldInformation
    {
        /// <summary>
        ///     Type of response.
        /// </summary>
        public Type Response { get; set; }

        /// <summary>
        ///     Name of field.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Query arguments.
        /// </summary>
        public QueryArguments Arguments { get; set; }

        public bool IsMutation { get; set; }
    }
}
