#nullable enable

using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list type node within a document.
    /// </summary>
    public class ListType : AbstractNode, IType
    {
        /// <summary>
        /// Initializes a list type node that wraps the specified type node.
        /// </summary>
        public ListType(IType type)
        {
            Type = type;
        }

        /// <summary>
        /// Returns the wrapped type node.
        /// </summary>
        public IType Type { get; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get { yield return Type; }
        }

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state) => action(Type, state);

        /// <inheritdoc/>
        public override string ToString() => $"ListType{{type={Type}}}";
    }
}
