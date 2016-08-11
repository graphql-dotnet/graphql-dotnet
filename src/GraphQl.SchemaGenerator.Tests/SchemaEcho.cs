using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.SchemaGenerator.Attributes;

namespace GraphQL.SchemaGenerator.Tests
{
    [GraphType]
    public class SchemaEcho
    {
        [GraphRoute]
        public SchemaResponse TestRequest(Schema1Request request)
        {
            return new SchemaResponse
            {
                Value = request?.Echo ?? 5
            };
        }


        [GraphRoute]
        public IEnumerable<SchemaResponse> TestEnumerable(Schema1Request request)
        {
            return new List<SchemaResponse>
            {
                new SchemaResponse
                {
                    Value = 1
                },
                new SchemaResponse
                {
                    Value = request?.Echo ?? 5
                },
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
