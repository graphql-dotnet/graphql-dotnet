using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public class UnionGraphType : GraphType, IAbstractGraphType
    {
        private List<Type> _types;
        private List<IObjectGraphType> _possibleTypes;

        public IEnumerable<IObjectGraphType> PossibleTypes
        {
            get => _possibleTypes ?? Enumerable.Empty<IObjectGraphType>();
            set
            {
                EnsurePossibleTypes();

                _possibleTypes.Clear();
                _possibleTypes.AddRange(value);
            }
        }

        public Func<object, IObjectGraphType> ResolveType { get; set; }

        public void AddPossibleType(IObjectGraphType type)
        {
            EnsurePossibleTypes();

            if (type != null && !_possibleTypes.Contains(type))
            {
                _possibleTypes.Add(type);
            }
        }

        public IEnumerable<Type> Types
        {
            get => _types ?? Enumerable.Empty<Type>();
            set
            {
                EnsureTypes();

                _types.Clear();
                _types.AddRange(value);
            }
        }

        public void Type<TType>()
            where TType : IObjectGraphType
        {
            EnsureTypes();

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

            EnsureTypes();

            if (!_types.Contains(type))
                _types.Add(type);
        }

        private void EnsureTypes()
        {
            if (_types == null)
                _types = new List<Type>();
        }

        private void EnsurePossibleTypes()
        {
            if (_possibleTypes == null)
                _possibleTypes = new List<IObjectGraphType>();
        }
    }
}
