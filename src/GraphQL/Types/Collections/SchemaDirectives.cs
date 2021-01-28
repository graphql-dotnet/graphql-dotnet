using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a list of directives supported by the schema.
    /// </summary>
    public class SchemaDirectives : IEnumerable<DirectiveGraphType>
    {
        internal List<DirectiveGraphType> List { get; } = new List<DirectiveGraphType>();

        /// <summary>
        /// Gets the count of directives.
        /// </summary>
        public int Count => List.Count;

        internal void Add(DirectiveGraphType directive)
        {
            if (directive == null)
                throw new ArgumentNullException(nameof(directive));

            if (!List.Contains(directive))
                List.Add(directive);
        }

        public bool Contains(DirectiveGraphType type) => List.Contains(type ?? throw new ArgumentNullException(nameof(type)));

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<DirectiveGraphType> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
