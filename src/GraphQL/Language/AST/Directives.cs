using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a node containing a list of directive nodes within a document.
    /// </summary>
    public class Directives : AbstractNode, ICollection<Directive>
    {
        private List<Directive> _directives;
        private readonly Dictionary<string, Directive> _unique = new Dictionary<string, Directive>(StringComparer.Ordinal);

        internal Directives(int capacity)
        {
            _directives = new List<Directive>(capacity);
        }

        /// <summary>
        /// Creates an instance of directives node.
        /// </summary>
        public Directives()
        {
        }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children => _directives;

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            if (_directives != null)
            {
                foreach (var directive in _directives)
                    action(directive, state);
            }
        }

        /// <summary>
        /// Adds a directive node to the list.
        /// </summary>
        public void Add(Directive directive)
        {
            (_directives ??= new List<Directive>()).Add(directive ?? throw new ArgumentNullException(nameof(directive)));

            if (!_unique.ContainsKey(directive.Name))
            {
                _unique.Add(directive.Name, directive);
            }
        }

        /// <summary>
        /// Searches the list for a directive node specified by name and returns it.
        /// </summary>
        public Directive Find(string name) => _unique.TryGetValue(name, out Directive value) ? value : null;

        /// <summary>
        /// Returns the number of directive nodes in this list.
        /// </summary>
        public int Count => _directives?.Count ?? 0;

        /// <summary>
        /// Returns <see langword="true"/> if there are any duplicate directive nodes in this list when compared by name.
        /// </summary>
        public bool HasDuplicates => _directives?.Count != _unique.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public IEnumerator<Directive> GetEnumerator() => (_directives ?? System.Linq.Enumerable.Empty<Directive>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public void Clear()
        {
            _directives.Clear();
            _unique.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(Directive item) => _directives.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(Directive[] array, int arrayIndex) => _directives.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public bool Remove(Directive item) => _directives.Remove(item) && _unique.Remove(item.Name);

        /// <inheritdoc />
        public override string ToString() => _directives?.Count > 0 ? $"Directives{{{string.Join(", ", _directives)}}}" : "Directives(Empty)";
    }
}
