using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IInputObjectGraphType : IInputGraphType
    {
        IEnumerable<FieldType> Fields { get; }
    }
}