using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL.Types
{
    public class UnionGraphType : GraphType, IAbstractGraphType
    {
        private readonly List<Type> _types;
        private readonly List<IObjectGraphType> _possibleTypes;

        public IEnumerable<IObjectGraphType> PossibleTypes => _possibleTypes;

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
            _types.Add(typeof(TType));
        }

        public void Type(Type type)
        {
            if (!type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IObjectGraphType)))
            {
                throw new ArgumentException($"Added union type must implement {nameof(IObjectGraphType)}", nameof(type));
            }
            _types.Add(type);
        }
    }
}
