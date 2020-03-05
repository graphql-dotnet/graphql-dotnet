using System;
using System.Collections;
using System.Collections.Generic;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    public static class QueryArgumentsExtensions
    {
        public static IQueryArgument Find(this IEnumerable<IQueryArgument> list, string name)
        {
            if (list == null || (list is IReadOnlyCollection<IQueryArgument> queryArguments && queryArguments.Count == 0))
                return null;

            foreach (var arg in list)
                if (arg.Name == name)
                    return arg;

            return null;
        }
    }

    public class QueryArguments : List<QueryArgument>
    {
        public QueryArguments(params QueryArgument[] args) : base(args) { }

        public QueryArguments(IEnumerable<QueryArgument> list) : base(list) { }

        public new QueryArgument this[int index]
        {
            get => base[index];
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                NameValidator.ValidateName(value.Name, "argument");

                base[index] = value;
            }
        }

        public new void Add(QueryArgument argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            NameValidator.ValidateName(argument.Name, "argument");

            base.Add(argument);
        }

        public new void AddRange(IEnumerable<QueryArgument> arguments)
        {
            foreach (var argument in arguments)
                Add(argument);
        }

        public new void Insert(int index, QueryArgument argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            NameValidator.ValidateName(argument.Name, "argument");

            base.Insert(index, argument);
        }

        public new void InsertRange(int index, IEnumerable<QueryArgument> arguments)
        {
            foreach (var argument in arguments)
            {
                if (argument == null)
                    throw new ArgumentNullException(nameof(argument), "One of the arguments in the collection was null");

                NameValidator.ValidateName(argument.Name, "argument");
            }

            base.InsertRange(index, arguments);
        }

        public QueryArgument Find(string name)
        {
            if (Count == 0)
                return null;

            foreach (var arg in this)
                if (arg.Name == name)
                    return arg;

            return null;
        }
    }
}
