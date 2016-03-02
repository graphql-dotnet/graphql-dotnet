using System;
using System.Collections.Generic;
using GraphQL.Execution;

namespace GraphQL.Types
{
    public class UnionGraphType : GraphQLAbstractType
    {
        private readonly List<Type> _types = new List<Type>();

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
            where TType : GraphType
        {
            _types.Add(typeof(TType));
        }

        public override bool IsPossibleType(ExecutionContext context, GraphType type)
        {
            var graphTypes = context.Schema.FindTypes(Types);
            return graphTypes.Any(x => x.Equals(type));
        }
    }
}
