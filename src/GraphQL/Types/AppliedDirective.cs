using System.Collections.Generic;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a directive applied to a schema element - type, field, argument, etc.
    /// </summary>
    public class AppliedDirective
    {
        /// <summary>
        /// Creates directive.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public AppliedDirective(string name)
        {
            Name = name;
        }

        private string _name;

        /// <summary>
        /// Directive name.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                NameValidator.ValidateName(value, "directive");
                _name = value;
            }
        }

        /// <summary>
        /// Searches the directive arguments for an argument specified by its name and returns it.
        /// </summary>
        /// <param name="argumentName">Argument name.</param>
        public DirectiveArgument Find(string argumentName)
        {
            if (Arguments != null)
            {
                foreach (var arg in Arguments)
                {
                    if (arg.Name == argumentName)
                        return arg;
                }
            }

            return null;
        }

        /// <summary>
        /// Directive arguments.
        /// </summary>
        public List<DirectiveArgument> Arguments { get; set; }
    }
}
