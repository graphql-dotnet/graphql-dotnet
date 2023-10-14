using System.Collections;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a directive applied to a schema element - type, field, argument, etc.
    /// </summary>
    public class AppliedDirective : IEnumerable<DirectiveArgument>
    {
        private string _name = null!;

        internal List<DirectiveArgument>? List { get; private set; }

        /// <summary>
        /// Creates directive.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public AppliedDirective(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Directive name.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                NameValidator.ValidateName(value, NamedElement.Directive);
                _name = value;
            }
        }

        /// <summary>
        /// Returns the number of directive arguments.
        /// </summary>
        public int ArgumentsCount => List?.Count ?? 0;

        /// <summary>
        /// Adds an argument to the directive.
        /// </summary>
        public AppliedDirective AddArgument(DirectiveArgument argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            (List ??= new()).Add(argument);

            return this;
        }

        /// <summary>
        /// Searches the directive arguments for an argument specified by its name and returns it.
        /// </summary>
        /// <param name="argumentName">Argument name.</param>
        public DirectiveArgument? FindArgument(string argumentName)
        {
            if (List != null)
            {
                foreach (var arg in List)
                {
                    if (arg.Name == argumentName)
                        return arg;
                }
            }

            return null;
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<DirectiveArgument> GetEnumerator() => (List ?? System.Linq.Enumerable.Empty<DirectiveArgument>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
