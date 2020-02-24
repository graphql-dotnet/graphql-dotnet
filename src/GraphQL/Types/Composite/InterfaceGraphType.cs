using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IInterfaceGraphType : IAbstractGraphType, IComplexGraphType
    {
    }

    public class InterfaceGraphType<TSource> : ComplexGraphType<TSource>, IInterfaceGraphType
    {
        private readonly List<IObjectGraphType> _possibleTypes = new List<IObjectGraphType>();

        public IEnumerable<IObjectGraphType> PossibleTypes => _possibleTypes;

        public Func<object, IObjectGraphType> ResolveType { get; set; }

        public void AddPossibleType(IObjectGraphType type)
        {
            if (!_possibleTypes.Contains(type))
            {
                this.IsValidInterfaceFor(type, throwError: true);
                _possibleTypes.Add(type ?? throw new ArgumentNullException(nameof(type)));
            }
        }
    }

    public class InterfaceGraphType : InterfaceGraphType<object>
    {
    }
}
