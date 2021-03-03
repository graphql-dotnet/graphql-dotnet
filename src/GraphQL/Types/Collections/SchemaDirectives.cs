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

        /// <summary>
        /// Register the specified directive to the schema.
        /// <br/><br/>
        /// Directives are used by the GraphQL runtime as a way of modifying execution
        /// behavior. Type system creators do not usually create them directly.
        /// </summary>
        public void Register(DirectiveGraphType directive)
        {
            if (directive == null)
                throw new ArgumentNullException(nameof(directive));

            if (!List.Contains(directive))
                List.Add(directive);
        }

        /// <summary>
        /// Register the specified directives to the schema.
        /// <br/><br/>
        /// Directives are used by the GraphQL runtime as a way of modifying execution
        /// behavior. Type system creators do not usually create them directly.
        /// </summary>
        public void Register(params DirectiveGraphType[] directives)
        {
            foreach (var directive in directives)
                Register(directive);
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

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<DirectiveGraphType> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
