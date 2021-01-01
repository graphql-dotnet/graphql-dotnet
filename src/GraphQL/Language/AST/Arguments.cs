using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Arguments : AbstractNode, IEnumerable<Argument>
    {
        private List<Argument> _arguments;
        // for internal use only, do not modify this instance
        internal static readonly Arguments Empty = new Arguments();

        public override IEnumerable<INode> Children => _arguments;

        public void Add(Argument arg)
        {
            if (arg == null)
                throw new ArgumentNullException(nameof(arg));

            if (_arguments == null)
                _arguments = new List<Argument>();

            _arguments.Add(arg);
        }

        public IValue ValueFor(string name)
        {
            if (_arguments == null)
                return null;

            // DO NOT USE LINQ ON HOT PATH
            foreach (var x in _arguments)
                if (x.Name == name)
                    return x.Value;

            return null;
        }

        public override bool IsEqualTo(INode obj) => ReferenceEquals(this, obj);

        public IEnumerator<Argument> GetEnumerator()
        {
            if (_arguments == null)
                return System.Linq.Enumerable.Empty<Argument>().GetEnumerator();

            return _arguments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public override string ToString() => _arguments?.Count > 0 ? $"Arguments{{{string.Join(", ", _arguments)}}}" : "Arguments(Empty)";
    }
}
