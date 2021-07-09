#nullable enable

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
        private List<Argument>? _arguments;
        // for internal use only, do not modify this instance
        internal static readonly Arguments Empty = new Arguments();

        internal Arguments(int capacity)
        {
            _arguments = new List<Argument>(capacity);
        }

        /// <summary>
        /// Gets the count of argument nodes.
        /// </summary>
        public int Count => _arguments?.Count ?? 0;

        /// <summary>
        /// Creates an instance of a list of argument nodes.
        /// </summary>
        public Arguments()
        {
        }

        /// <inheritdoc/>
        public override IEnumerable<INode>? Children => _arguments;

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            if (_arguments != null)
            {
                foreach (var arg in _arguments)
                    action(arg, state);
            }
        }

        /// <summary>
        /// Adds an argument node to the list.
        /// </summary>
        public void Add(Argument arg) => (_arguments ??= new List<Argument>()).Add(arg ?? throw new ArgumentNullException(nameof(arg)));

        /// <summary>
        /// Returns the value of an argument node, searching the list of argument nodes by the name of the argument.
        /// </summary>
        public IValue? ValueFor(string name)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (_arguments != null)
            {
                foreach (var x in _arguments)
                {
                    if (x.Name == name)
                        return x.Value;
                }
            }

            return null;
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<Argument> GetEnumerator() => (_arguments ?? System.Linq.Enumerable.Empty<Argument>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public override string ToString() => _arguments?.Count > 0 ? $"Arguments{{{string.Join(", ", _arguments)}}}" : "Arguments(Empty)";
    }
}
