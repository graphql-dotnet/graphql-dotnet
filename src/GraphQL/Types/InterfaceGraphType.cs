using GraphQL.Builders;
using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IInterfaceGraphType : IAbstractGraphType, IComplexGraphType
    {
    }

    public class InterfaceGraphType : ComplexGraphType<object>, IInterfaceGraphType
    {
        private readonly List<IObjectGraphType> _possibleTypes = new List<IObjectGraphType>();

        public IEnumerable<IObjectGraphType> PossibleTypes => _possibleTypes;

        public Func<object, IObjectGraphType> ResolveType { get; set; }

        public void AddPossibleType(IObjectGraphType type)
        {
            if (type != null && !_possibleTypes.Contains(type))
            {
                _possibleTypes.Add(type);
            }
        }
    }

    public class InterfaceGraphType<TSource> : ComplexGraphType<TSource>, IInterfaceGraphType
    {
        private readonly List<IObjectGraphType> _possibleTypes = new List<IObjectGraphType>();

        public IEnumerable<IObjectGraphType> PossibleTypes => _possibleTypes;

        public Func<object, IObjectGraphType> ResolveType { get; set; }

        public void AddPossibleType(IObjectGraphType type)
        {
            if (type != null && !_possibleTypes.Contains(type))
            {
                _possibleTypes.Add(type);
            }
        }
    }
}
