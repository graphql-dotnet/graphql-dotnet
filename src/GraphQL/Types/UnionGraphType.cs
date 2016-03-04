using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public class UnionGraphType : GraphQLAbstractType
    {
        private readonly List<Type> _types;

        public UnionGraphType()
        {
            _types = new List<Type>();
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
