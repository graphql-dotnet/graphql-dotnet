using System.Collections;
using GraphQLParser;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a list of directives supported by the schema.
    /// </summary>
    public class SchemaDirectives : IEnumerable<Directive>
    {
        internal List<Directive> List { get; } = new List<Directive>();

        /// <summary>
        /// Returns an instance of the predefined 'include' directive.
        /// </summary>
        public virtual IncludeDirective Include { get; } = new IncludeDirective();

        /// <summary>
        /// Returns an instance of the predefined 'skip' directive.
        /// </summary>
        public virtual SkipDirective Skip { get; } = new SkipDirective();

        /// <summary>
        /// Returns an instance of the predefined 'deprecated' directive.
        /// </summary>
        public virtual DeprecatedDirective Deprecated { get; } = new DeprecatedDirective();

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
        public void Register(Directive directive)
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
        public void Register(params Directive[] directives)
        {
            foreach (var directive in directives)
                Register(directive);
        }

        /// <summary>
        /// Searches the directive by its name and returns it.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public Directive? Find(ROM name)
        {
            foreach (var directive in List)
            {
                if (directive.Name == name)
                    return directive;
            }

            return null;
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<Directive> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
