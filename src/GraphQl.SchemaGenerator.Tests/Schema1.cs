using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.SchemaGenerator.Attributes;

namespace GraphQL.SchemaGenerator.Tests
{
    [GraphType]
    public class Schema1
    {
        [GraphRoute]
        public SchemaResponse TestRequest(Schema1Request request)
        {
            return new SchemaResponse
            {
                Value = request?.Echo ?? 5
            };
        }
    }

    public class Schema1Request
    {
        public int? Echo { get; set; }
        public string Data { get; set; }
    }

    public class SchemaResponse
    {
        public int Value { get; set; }
    }
}
