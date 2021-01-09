using System;
using System.Collections;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <inheritdoc cref="ListGraphType"/>
    public class ListGraphType<T> : ListGraphType
        where T : IGraphType
    {
        /// <inheritdoc cref="ListGraphType.ListGraphType(Type)"/>
        public ListGraphType()
            : base(typeof(T))
        {
        }
    }

    /// <summary>
    /// Represents a list of objects. A GraphQL schema may describe that a field represents a list of another type.
    /// The List type is provided for this reason, and wraps another type.
    /// </summary>
    public class ListGraphType : GraphType, IProvideResolvedType
    {
        /// <summary>
        /// Initializes a new instance for the specified inner graph type.
        /// </summary>
        public ListGraphType(IGraphType type)
        {
            ResolvedType = type;
        }

        /// <inheritdoc cref="ListGraphType.ListGraphType(IGraphType)"/>
        protected ListGraphType(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Returns the .NET type of the inner (wrapped) graph type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets or sets the instance of the inner (wrapped) graph type.
        /// </summary>
        public IGraphType ResolvedType { get; set; }

        /*
        /// <inheritdoc/>
        public virtual object ParseLiteral(IValue value)
        {
            if (value == null || value is NullValue)
                return null;

            if (!(ResolvedType is IInputType resolvedInputType))
                throw new InvalidOperationException("The underlying graph type is not an input graph type.");

            if (!(value is ListValue listValue))
                throw new ArgumentOutOfRangeException("The value supplied is not a list node.");

            var valuesList = listValue.ValuesList;
            var values = new List<object>(valuesList.Count);
            for (int i = 0; i < values.Count; i++)
            {
                values[i] = resolvedInputType.ParseLiteral(valuesList[i]);
            }
            return values;
        }
        */

        /// <inheritdoc/>
        public override string ToString() => $"[{ResolvedType}]";
    }
}
