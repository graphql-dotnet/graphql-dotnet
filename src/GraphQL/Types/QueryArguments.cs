using System;
using System.Collections;
using System.Collections.Generic;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    public class QueryArguments : IReadOnlyCollection<QueryArgument>, ICollection
    {
        public QueryArguments(params QueryArgument[] args)
        {
            foreach (var arg in args)
            {
                Add(arg);
            }
        }

        public QueryArguments(IEnumerable<QueryArgument> list)
        {
            foreach (var arg in list)
            {
                Add(arg);
            }
        }

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

        public int Count => ArgumentsList?.Count ?? 0;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        public void Add(QueryArgument argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            NameValidator.ValidateName(argument.Name, "argument");

            if (ArgumentsList == null)
                ArgumentsList = new List<QueryArgument>();

            ArgumentsList.Add(argument);
        }

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

        public IEnumerator<QueryArgument> GetEnumerator()
        {
            if (ArgumentsList == null)
                return System.Linq.Enumerable.Empty<QueryArgument>().GetEnumerator();

            return ArgumentsList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index) => ((ICollection)ArgumentsList)?.CopyTo(array, index);
    }
}
