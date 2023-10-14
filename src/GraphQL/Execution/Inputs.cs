using System.Collections.ObjectModel;
using GraphQL.Validation;

namespace GraphQL
{
    /// <summary>
    /// Represents a readonly dictionary of variable inputs to a document. Typically this
    /// contains the deserialized 'variables' property from the GraphQL request. During document execution,
    /// these inputs will be validated and coerced into a <see cref="Variables"/> dictionary.
    /// </summary>
    public class Inputs : ReadOnlyDictionary<string, object?>
    {
        /// <summary>
        /// Returns an empty set of inputs.
        /// </summary>
        public static readonly Inputs Empty = new(new Dictionary<string, object?>());

        /// <summary>
        /// Initializes a new instance that is a wrapper for the specified dictionary of elements.
        /// </summary>
        public Inputs(IDictionary<string, object?> dictionary)
            : base(dictionary)
        {
        }
    }
}
