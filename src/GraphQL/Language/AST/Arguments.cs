using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of argument nodes.
    /// </summary>
    public class Arguments : AbstractNode, IEnumerable<Argument>
    {
        private List<Argument> _arguments;
        // for internal use only, do not modify this instance
        internal static readonly Arguments Empty = new Arguments();

        /// <inheritdoc/>
        public override IEnumerable<INode> Children => _arguments;

        /// <summary>
        /// Adds an argument node to the list.
        /// </summary>
        public void Add(Argument arg)
        {
            if (arg == null)
                throw new ArgumentNullException(nameof(arg));

            _arguments ??= new List<Argument>();

            _arguments.Add(arg);
        }

        /// <summary>
        /// Returns the value of an argument node, searching the list of argument nodes by the name of the argument.
        /// </summary>
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

        /// <inheritdoc/>
        public override bool IsEqualTo(INode obj) => ReferenceEquals(this, obj);

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
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
