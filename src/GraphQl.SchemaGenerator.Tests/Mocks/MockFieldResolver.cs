using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQl.SchemaGenerator.Models;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator.Tests.Mocks
{
    class MockFieldResolver : IGraphFieldResolver
    {
        public object Data { get; set; }

        public MockFieldResolver(object data)
        {
            Data = data;
        }

        public object ResolveField(ResolveFieldContext context, FieldInformation route)
        {
            return Data;
        }
    }
}
