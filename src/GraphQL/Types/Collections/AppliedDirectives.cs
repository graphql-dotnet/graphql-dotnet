using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a list of directives applied to a schema element (type, field, argument, etc.).
    /// </summary>
    public class AppliedDirectives : IEnumerable<AppliedDirective>
    {
        internal List<AppliedDirective> List { get; } = new List<AppliedDirective>();

        /// <summary>
        /// Gets the count of applied directives.
        /// </summary>
        public int Count => List.Count;

        /// <summary>
        /// Adds directive to list.
        /// </summary>
        public void Add(AppliedDirective directive)
        {
            if (directive == null)
                throw new ArgumentNullException(nameof(directive));

            if (!List.Contains(directive))
                List.Add(directive);
        }

        public bool Contains(AppliedDirective directive) => List.Contains(directive ?? throw new ArgumentNullException(nameof(directive)));

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<AppliedDirective> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
