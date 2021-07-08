#nullable enable

using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value node which contains a literal value within a document.
    /// </summary>
    public abstract class ValueNode<T> : AbstractNode, IValue<T>
    {
        [Obsolete]
        public ValueNode() : this(default!)
        {
        }

        public ValueNode(T value)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            Value = value;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <inheritdoc/>
        public T Value
        {
            get;
            [Obsolete]
            protected set;
        }

        object IValue.Value => Value!;

        /// <inheritdoc/>
        public override string ToString() => $"{GetType().Name}{{value={Value}}}";
    }
}
