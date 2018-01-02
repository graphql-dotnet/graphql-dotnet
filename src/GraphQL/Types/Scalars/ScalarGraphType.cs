using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{

    /// <summary>
    /// Represents a primitive value in GraphQL.
    /// GraphQL responses take the form of a hierarchical tree; the leaves on these trees are GraphQL scalars.
    /// </summary>
    /// <typeparam name="Type">The Primitive Type</typeparam>
    public abstract class ScalarGraphType<Type> : GraphType
    {
        public abstract object Serialize(object value);

        public abstract Type ParseValue(object value);

        public abstract object ParseLiteral(IValue value);
    }

    [Obsolete("Deprecated in favour of ScalarGraphType<Type>")]
    public abstract class ScalarGraphType : GraphType
    {
        public abstract object Serialize(object value);

        public abstract object ParseValue(object value);

        public abstract object ParseLiteral(IValue value);
    }
}
