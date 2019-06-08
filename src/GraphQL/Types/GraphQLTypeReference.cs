using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public class GraphQLTypeReference : InterfaceGraphType, IObjectGraphType
    {
        public GraphQLTypeReference(string typeName)
        {
            Name = "__GraphQLTypeReference";
            TypeName = typeName;
        }

        public string TypeName { get; private set; }

        public Func<object, bool> IsTypeOf
        {
             get => throw Invalid();
             set => throw Invalid();
        }

        public void AddResolvedInterface(IInterfaceGraphType graphType) => throw Invalid();

        public IEnumerable<Type> Interfaces
        {
             get => throw Invalid();
             set => throw Invalid();
        }

        public IEnumerable<IInterfaceGraphType> ResolvedInterfaces
        {
            get => throw Invalid();
            set => throw Invalid();
        }

        private Exception Invalid() => new InvalidOperationException("This is just a reference. Resolve the real type first.");

        public override bool Equals(object obj)
        {
            if (obj is GraphQLTypeReference other)
            {
                return TypeName == other.TypeName;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode() => TypeName?.GetHashCode() ?? 0;
    }
}
