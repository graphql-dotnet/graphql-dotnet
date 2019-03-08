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

        public Type SourceType => throw new InvalidOperationException("This is just a reference. Resolve the real type first.");

        public string TypeName { get; private set; }

        public Func<object, bool> IsTypeOf
        {
            get
            {
                throw new InvalidOperationException("This is just a reference. Resolve the real type first.");
            }
            set
            {
                throw new InvalidOperationException("This is just a reference. Resolve the real type first.");
            }
        }

        public void AddResolvedInterface(IInterfaceGraphType graphType)
        {
            throw new InvalidOperationException("This is just a reference. Resolve the real type first.");
        }

        public IEnumerable<Type> Interfaces
        {
            get
            {
                throw new InvalidOperationException("This is just a reference. Resolve the real type first.");
            }
            set
            {
                throw new InvalidOperationException("This is just a reference. Resolve the real type first.");
            }
        }

        public IEnumerable<IInterfaceGraphType> ResolvedInterfaces
        {
            get
            {
                throw new InvalidOperationException("This is just a reference. Resolve the real type first.");
            }
            set
            {
                throw new InvalidOperationException("This is just a reference. Resolve the real type first.");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is GraphQLTypeReference other)
            {
                return TypeName == other.TypeName;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return TypeName?.GetHashCode() ?? 0;
        }
    }
}
