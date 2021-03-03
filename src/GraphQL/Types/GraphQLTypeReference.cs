using System;
using System.Diagnostics;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a placeholder for another GraphQL type, referenced by name. Must be replaced with a
    /// reference to the actual GraphQL type before using the reference.
    /// </summary>
    [DebuggerDisplay("ref {TypeName,nq}")]
    internal sealed class GraphQLTypeReference : InterfaceGraphType, IObjectGraphType
    {
        public GraphQLTypeReference(string typeName)
        {
            SetName("__GraphQLTypeReference", validate: false);
            TypeName = typeName;
        }

        /// <summary>
        /// Returns the GraphQL type name that this reference is a placeholder for.
        /// </summary>
        public string TypeName { get; private set; }

        public Func<object, bool> IsTypeOf
        {
            get => throw Invalid();
            set => throw Invalid();
        }

        public void AddResolvedInterface(IInterfaceGraphType graphType) => throw Invalid();

        public Interfaces Interfaces => throw Invalid();

        public ResolvedInterfaces ResolvedInterfaces => throw Invalid();

        private InvalidOperationException Invalid() => new InvalidOperationException($"This is just a reference to '{TypeName}'. Resolve the real type first.");

        public override bool Equals(object obj)
        {
            return obj is GraphQLTypeReference other
                ? TypeName == other.TypeName
                : base.Equals(obj);
        }

        public override int GetHashCode() => TypeName?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Represents a placeholder for another GraphQL Output type, referenced by CLR type. Must be replaced with a
    /// reference to the actual GraphQL type before using the reference.
    /// </summary>
    internal sealed class GraphQLClrOutputTypeReference<T> : InterfaceGraphType
    {
        internal GraphQLClrOutputTypeReference()
        {
            throw new InvalidOperationException("Not for creation. Marker only.");
        }
    }

    /// <summary>
    /// Represents a placeholder for another GraphQL Input type, referenced by CLR type. Must be replaced with a
    /// reference to the actual GraphQL type before using the reference.
    /// </summary>
    internal sealed class GraphQLClrInputTypeReference<T> : InputObjectGraphType
    {
        internal GraphQLClrInputTypeReference()
        {
            throw new InvalidOperationException("Not for creation. Marker only.");
        }
    }
}
