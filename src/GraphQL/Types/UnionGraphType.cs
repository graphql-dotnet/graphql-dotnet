using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public class UnionGraphType : GraphType, IAbstractGraphType
    {
        private readonly List<IGraphType> _types;
        private readonly List<IObjectGraphType> _possibleTypes;

        public IEnumerable<IObjectGraphType> PossibleTypes => _possibleTypes;

        public Func<object, IObjectGraphType> ResolveType { get; set; }

        public UnionGraphType()
        {
            _types = new List<IGraphType>();
            _possibleTypes = new List<IObjectGraphType>();
        }

        public void AddPossibleType(IObjectGraphType type)
        {
            if (type != null && !_possibleTypes.Contains(type))
            {
                _possibleTypes.Add(type);
            }
        }

        public IEnumerable<IGraphType> Types
        {
            get { return _types; }
            set
            {
                _types.Clear();
                _types.AddRange(value);
            }
        }

        public void Type(IGraphType type)
        {
            _types.Add(type);
        }
    }
}
