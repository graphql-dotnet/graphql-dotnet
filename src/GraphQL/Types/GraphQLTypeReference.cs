using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a placeholder for another GraphQL type, referenced by name. Must be replaced with a
    /// reference to the actual GraphQL type before using the reference.
    /// </summary>
    [DebuggerDisplay("ref {TypeName,nq}")]
    public class GraphQLTypeReference : InterfaceGraphType, IObjectGraphType
    {
        /// <summary>
        /// Initializes a new instance for the specified GraphQL type name.
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
        public Func<object, bool> IsTypeOf
        {
            get => throw Invalid();
            set => throw Invalid();
        }

        /// <inheritdoc/>
        public void AddResolvedInterface(IInterfaceGraphType graphType) => throw Invalid();

        /// <inheritdoc/>
        public IEnumerable<Type> Interfaces
        {
            get => throw Invalid();
            set => throw Invalid();
        }

        /// <inheritdoc/>
        public IEnumerable<IInterfaceGraphType> ResolvedInterfaces
        {
            get => throw Invalid();
            set => throw Invalid();
        }

        private InvalidOperationException Invalid() => new InvalidOperationException(
            $"This is just a reference to '{TypeName}'. Resolve the real type first.");

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is GraphQLTypeReference other)
            {
                return TypeName == other.TypeName;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => TypeName?.GetHashCode() ?? 0;
    }
}
