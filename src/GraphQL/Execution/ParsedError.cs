using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents the fields of a GraphQL error entry.  See https://spec.graphql.org/June2018/#sec-Errors
    /// <br/><br/>
    /// Note that the field names must be encoded as camelCase
    /// </summary>
    public class ParsedError
    {
        // The following descriptions are copied from https://spec.graphql.org/June2018/#sec-Errors

        /// <summary>
        /// A description of the error intended for the developer as a guide to understand and correct the error
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// A list of locations, where each location is a map with the keys 'line' and 'column', both positive numbers
        /// starting from 1 which describe the beginning of an associated syntax element
        /// </summary>
        public IEnumerable<ErrorLocation> Locations { get; set; }

        /// <summary>
        /// A list of path segments starting at the root of the response and ending with the field associated with the error.
        /// Path segments that represent fields should be strings, and path segments that represent list indices should be 0‐indexed integers.
        /// If the error happens in an aliased field, the path to the error should use the aliased name, since it represents a path in the response, not in the query.
        /// </summary>
        public IEnumerable<object> Path { get; set; }

        /// <summary>
        /// This entry, if set, must have a map as its value. This entry is reserved for implementors to add additional
        /// information to errors however they see fit, and there are no additional restrictions on its contents.
        /// </summary>
        public IDictionary<string, object> Extensions { get; set; }
    }
}
