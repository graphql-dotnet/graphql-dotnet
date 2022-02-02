using System.Diagnostics;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a placeholder for another GraphQL type, referenced by name. Must be replaced with a
    /// reference to the actual GraphQL type before using the reference.
    /// </summary>
    [DebuggerDisplay("ref {TypeName,nq}")]
    public sealed class GraphQLTypeReference : InterfaceGraphType, IObjectGraphType
    {
        /// <summary>
        /// Initializes a new instance for the specified graph type name.
        /// </summary>
        public GraphQLTypeReference(string typeName)
        {
            SetName("__GraphQLTypeReference", validate: false);
            TypeName = typeName;
        }

        /// <summary>
        /// Returns the GraphQL type name that this reference is a placeholder for.
        /// </summary>
        public string TypeName { get; private set; }

        /// <inheritdoc/>
        public Func<object, bool>? IsTypeOf
        {
            get => throw Invalid();
            set => throw Invalid();
        }

        /// <inheritdoc/>
        public void AddResolvedInterface(IInterfaceGraphType graphType) => throw Invalid();

        /// <inheritdoc/>
        public Interfaces Interfaces => throw Invalid();

        /// <inheritdoc/>
        public ResolvedInterfaces ResolvedInterfaces => throw Invalid();

        private InvalidOperationException Invalid() => new InvalidOperationException($"This is just a reference to '{TypeName}'. Resolve the real type first.");

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GraphQLTypeReference other
                ? TypeName == other.TypeName
                : base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => TypeName?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Represents a placeholder for another GraphQL Output type, referenced by CLR type. Must be replaced with a
    /// reference to the actual GraphQL type before using the reference.
    /// </summary>
    public sealed class GraphQLClrOutputTypeReference<T> : InterfaceGraphType
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
    public sealed class GraphQLClrInputTypeReference<T> : InputObjectGraphType
    {
        internal GraphQLClrInputTypeReference()
        {
            throw new InvalidOperationException("Not for creation. Marker only.");
        }
    }
}
