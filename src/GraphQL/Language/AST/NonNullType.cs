#nullable enable

using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a non-null type node within a document.
    /// </summary>
    public class NonNullType : AbstractNode, IType
    {
        /// <summary>
        /// Initializes a new instance that wraps the specified type node.
        /// </summary>
        public NonNullType(IType type)
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
        public override string ToString() => $"NonNullType{{type={Type}}}";
    }
}
