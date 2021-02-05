using System;
using System.Collections;
using System.Collections.Generic;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    // TODO: better to rename QueryArguments and QueryArgument to something like GraphQLArguments and GraphQLArgument.
    /// <summary>
    /// Represents a list of arguments to a field or directive.
    /// </summary>
    public class QueryArguments : IEnumerable<QueryArgument>
    {
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
            get => ArgumentsList != null ? ArgumentsList[index] : throw new IndexOutOfRangeException();
            set
            {
                if (value != null)
                {
                    NameValidator.ValidateName(value.Name, "argument");
                }

                if (ArgumentsList == null)
                    throw new IndexOutOfRangeException();

                ArgumentsList[index] = value;
            }
        }

        internal List<QueryArgument> ArgumentsList { get; private set; }

        /// <summary>
        /// Returns the number of arguments in the list.
        /// </summary>
        public int Count => ArgumentsList?.Count ?? 0;

        /// <summary>
        /// Adds an argument to the list.
        /// </summary>
        public void Add(QueryArgument argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            NameValidator.ValidateName(argument.Name, "argument");

            ArgumentsList ??= new List<QueryArgument>();

            ArgumentsList.Add(argument);
        }

        /// <summary>
        /// Finds an argument by its name from the list.
        /// </summary>
        public QueryArgument Find(string name)
        {
            if (ArgumentsList == null)
                return null;

            // DO NOT USE LINQ ON HOT PATH
            foreach (var arg in ArgumentsList)
                if (arg.Name == name)
                    return arg;

            return null;
        }

        /// <inheritdoc/>
        public IEnumerator<QueryArgument> GetEnumerator()
        {
            if (ArgumentsList == null)
                return System.Linq.Enumerable.Empty<QueryArgument>().GetEnumerator();

            return ArgumentsList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
