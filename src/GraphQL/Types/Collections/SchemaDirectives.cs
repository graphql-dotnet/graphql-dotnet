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

        internal void Clear() => List.Clear();

        internal void Add(DirectiveGraphType directive)
        {
            if (directive == null)
                throw new ArgumentNullException(nameof(directive));

            if (!List.Contains(directive))
                List.Add(directive);
        }

        /// <summary>
        /// Searches the directive by its name and returns it.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public DirectiveGraphType Find(string name)
        {
            foreach (var directive in List)
            {
                if (directive.Name == name)
                    return directive;
            }

            return null;
        }

        public bool Contains(DirectiveGraphType type) => List.Contains(type ?? throw new ArgumentNullException(nameof(type)));

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<DirectiveGraphType> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
