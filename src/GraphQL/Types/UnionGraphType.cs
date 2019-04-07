using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public class UnionGraphType : GraphType, IAbstractGraphType
    {
        private readonly List<Type> _types;
        private readonly List<IObjectGraphType> _possibleTypes;

        public IEnumerable<IObjectGraphType> PossibleTypes
        {
            get { return _possibleTypes; }
            set
            {
                _possibleTypes.Clear();
                _possibleTypes.AddRange(value);
            }
        }

        public Func<object, IObjectGraphType> ResolveType { get; set; }

        public UnionGraphType()
        {
            _types = new List<Type>();
            _possibleTypes = new List<IObjectGraphType>();
        }

        public void AddPossibleType(IObjectGraphType type)
        {
            if (type != null && !_possibleTypes.Contains(type))
            {
                _possibleTypes.Add(type);
            }
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
            where TType : IObjectGraphType
        {
            if (!_types.Contains(typeof(TType)))
                _types.Add(typeof(TType));
        }

        public void Type(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!type.GetInterfaces().Contains(typeof(IObjectGraphType)))
            {
                throw new ArgumentException($"Added union type must implement {nameof(IObjectGraphType)}", nameof(type));
            }

            if (!_types.Contains(type))
                _types.Add(type);
        }
    }
}
