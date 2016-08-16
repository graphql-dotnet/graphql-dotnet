using System;
using System.Collections.Generic;
using System.ComponentModel;
using GraphQL.SchemaGenerator.Attributes;
using GraphQL.StarWars;

namespace GraphQL.SchemaGenerator.Tests
{
    [GraphType]
    public class EchoSchema
    {
        [Description(@"Tests a variety or request and response types.{VerifyComment}")]
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

        [GraphRoute]
        public IResponse TestInterface()
        {
            return new SchemaResponse
            {
                Value = 8,
                Enum = Episode.JEDI
            };
        }
    }

    //todo: note sure why we had two types and what they are for.
    [GraphKnownType(typeof(SchemaResponse), typeof(SchemaResponse))]
    public interface IResponse
    {
        int Value { get; }
    }

    public class Schema1Request
    {
        public int? Echo { get; set; }
        public string Data { get; set; }

        public decimal? Decimal { get; set; }

        public IEnumerable<Schema1Request> ComplexRequests { get; set; }

        public InnerRequest InnerRequest { get; set; }
    }

    public class InnerRequest
    {
        public string InnerData { get; set; }
    }

    public class SchemaResponse : IResponse
    {
        public Episode Enum { get; set; } = Episode.NEWHOPE;

        public int Value { get; set; }

        public int? NullValue { get; } = null;

        public DateTimeOffset? Date { get; set; } = new DateTime(1999,1,1);

        public TimeSpan TimeSpan { get; set; }

        public byte[] ByteArray { get; set; }

        public Dictionary<string, Response2> Values { get; set; } = new Dictionary<string, Response2>
        {
            {"99", new Response2 {ComplicatedResponse = new Schema1Request {Data = "99", Echo = 99} } },
            {"59", new Response2 {ComplicatedResponse = new Schema1Request {Data = "59", Echo = 59} } },
            {"null", null}
        };
    }

    public class Response2
    {
        public Schema1Request ComplicatedResponse { get; set; } 
    }
}
