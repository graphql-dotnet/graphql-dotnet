using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a GraphQL interface graph type.
    /// </summary>
    public interface IInterfaceGraphType : IAbstractGraphType, IComplexGraphType
    {
    }

    /// <inheritdoc cref="InterfaceGraphType"/>
    public class InterfaceGraphType<TSource> : ComplexGraphType<TSource>, IInterfaceGraphType
    {
        private readonly List<IObjectGraphType> _possibleTypes = new List<IObjectGraphType>();

        /// <inheritdoc/>
        public IEnumerable<IObjectGraphType> PossibleTypes => _possibleTypes;

        /// <inheritdoc/>
        public Func<object, IObjectGraphType> ResolveType { get; set; }

        /// <inheritdoc/>
        public void AddPossibleType(IObjectGraphType type)
        {
            if (!_possibleTypes.Contains(type))
            {
                this.IsValidInterfaceFor(type, throwError: true);
                _possibleTypes.Add(type ?? throw new ArgumentNullException(nameof(type)));
            }
        }
    }

    /// <inheritdoc cref="IInterfaceGraphType"/>
    public class InterfaceGraphType : InterfaceGraphType<object>
    {
    }
}
