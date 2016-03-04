using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public abstract class GraphQLAbstractType : GraphType
    {
        private readonly List<ObjectGraphType> _possibleTypes;

        protected GraphQLAbstractType()
        {
            _possibleTypes = new List<ObjectGraphType>();
        }

        public Func<object, ObjectGraphType> ResolveType { get; set; }

        public virtual IEnumerable<ObjectGraphType> PossibleTypes
        {
            get
            {
                return _possibleTypes;
            }
        }

        public virtual void AddPossibleType(ObjectGraphType type)
        {
            if (type != null && !_possibleTypes.Contains(type))
            {
                _possibleTypes.Add(type);
            }
        }

        public virtual bool IsPossibleType(GraphType type)
        {
            return PossibleTypes.Any(x => x.Equals(type));
        }

        public virtual ObjectGraphType GetObjectType(object value)
        {
            return ResolveType != null ? ResolveType(value) : GetTypeOf(value);
        }

        public virtual ObjectGraphType GetTypeOf(object value)
        {
            return PossibleTypes.FirstOrDefault(type => type.IsTypeOf != null && type.IsTypeOf(value));
        }
    }
}
