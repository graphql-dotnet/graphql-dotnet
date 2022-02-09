using System.Collections;
using GraphQL.Utilities;
using GraphQLParser;

namespace GraphQL.Types
{
    // TODO: better to rename QueryArguments and QueryArgument to something like GraphQLArguments and GraphQLArgument.
    /// <summary>
    /// Represents a list of arguments to a field or directive.
    /// </summary>
    public class QueryArguments : IEnumerable<QueryArgument>
    {
        internal List<QueryArgument>? List { get; private set; }

        /// <summary>
        /// Initializes a new instance containing the specified arguments.
        /// </summary>
        public QueryArguments(params QueryArgument[] args)
        {
            foreach (var arg in args)
            {
                Add(arg);
            }
        }

        /// <summary>
        /// Initializes a new instance containing the specified arguments.
        /// </summary>
        public QueryArguments(IEnumerable<QueryArgument> list)
        {
            foreach (var arg in list)
            {
                Add(arg);
            }
        }

        /// <summary>
        /// Gets or sets the argument at the specified index.
        /// </summary>
        public QueryArgument this[int index]
        {
            get => List != null ? List[index] : throw new IndexOutOfRangeException();
            set
            {
                if (value != null)
                {
                    NameValidator.ValidateName(value.Name, NamedElement.Argument);
                }

                if (List == null)
                    throw new IndexOutOfRangeException();

                List[index] = value!;
            }
        }

        /// <summary>
        /// Returns the number of arguments in the list.
        /// </summary>
        public int Count => List?.Count ?? 0;

        /// <summary>
        /// Adds an argument to the list.
        /// </summary>
        public void Add(QueryArgument argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            NameValidator.ValidateName(argument.Name, NamedElement.Argument);

            (List ??= new()).Add(argument);
        }

        /// <summary>
        /// Finds an argument by its name from the list.
        /// </summary>
        public QueryArgument? Find(ROM name)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (List != null)
            {
                foreach (var arg in List)
                {
                    if (arg.Name == name)
                        return arg;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public IEnumerator<QueryArgument> GetEnumerator() => (List ?? System.Linq.Enumerable.Empty<QueryArgument>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
