using GraphQL.Builders;
using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public class InterfaceGraphType : ComplexGraphType, IAbstractGraphType
    {
        private readonly List<ObjectGraphType> _possibleTypes = new List<ObjectGraphType>();

        public List<ObjectGraphType> PossibleTypes => _possibleTypes;

        public Func<object, ObjectGraphType> ResolveType { get; set; }

    }
}
