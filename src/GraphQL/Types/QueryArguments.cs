using GraphQL.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public class QueryArguments : IEnumerable<QueryArgument>
    {
        private List<QueryArgument> _arguments = new List<QueryArgument>();

        public QueryArguments(params QueryArgument[] args)
        {
            foreach(var arg in args)
            {
                Add(arg);
            }
        }

        public QueryArguments(IEnumerable<QueryArgument> list)
        {
            foreach(var arg in list)
            {
                Add(arg);
            }
        }

        public QueryArgument this[int index]
        {
            get => _arguments[index];
            set
            {
                if (value != null)
                {
                    NameValidator.ValidateName(value.Name, "argument");
                }

                _arguments[index] = value;
            }
        }

        public int Count => _arguments.Count;

        public void Add(QueryArgument argument)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            NameValidator.ValidateName(argument.Name, "argument");
            _arguments.Add(argument);
        }

        public QueryArgument Find(string name) => this.FirstOrDefault(x => x.Name == name);

        public IEnumerator<QueryArgument> GetEnumerator() => _arguments.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
