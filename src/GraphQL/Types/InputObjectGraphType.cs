using GraphQL.Builders;
using GraphQL.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GraphQL.Types
{
    public interface IInputObjectGraphType : IComplexGraphType
    {
    }

    public class InputObjectGraphType : InputObjectGraphType<object>
    {
    }

    public class InputObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IInputObjectGraphType
    {        

    }
}

