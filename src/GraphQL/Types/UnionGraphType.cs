using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public class UnionGraphType : GraphType, IAbstractGraphType
    {
        private readonly List<Type> _types;
        private readonly List<ObjectGraphType> _possibleTypes;

        public List<ObjectGraphType> PossibleTypes => _possibleTypes;

        public Func<object, ObjectGraphType> ResolveType { get; set; }

        public UnionGraphType()
        {
            _types = new List<Type>();
            _possibleTypes = new List<ObjectGraphType>();
        }

        public IEnumerable<Type> Types
        {
            get { return _types; }
            set
            {
                _types.Clear();
                _types.AddRange(value);
            }
        }

        public void Type<TType>()
            where TType : ObjectGraphType
        {
            _types.Add(typeof(TType));
        }
    }
}
