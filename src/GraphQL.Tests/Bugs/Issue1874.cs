using GraphQL.SystemTextJson;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Issue1874 : QueryTestBase<Issue1874Schema>
    {
        [Fact]
        public void byte_array_should_work()
        {
            var query = @"
                query BytesRequest($bytesHolder: Issue1874InputBytesType) {
                    bytes(bytesObject: $bytesHolder) {
                        bytes
                    }
                }";

            AssertQuerySuccess(query, @"{ ""bytes"": { ""bytes"": [1, 2, 3, 4] } }", @"{ ""bytesHolder"": { ""bytes"": [1, 2, 3, 4] } }".ToInputs());
        }
    }

    public class Issue1874Schema : Schema
    {
        public Issue1874Schema()
        {
            Query = new Issue1874Query();
        }
    }

    public class Issue1874Query : ObjectGraphType
    {
        public Issue1874Query()
        {
            Field<Issue1874OutputBytesType>(
                "bytes",
                arguments: new QueryArguments(new QueryArgument<Issue1874InputBytesType> { Name = "bytesObject" }),
                resolve: context =>
                {
                    var bytesObject = context.GetArgument<Issue1874BytesHolder>("bytesObject");
                    return bytesObject;
                }
            );
        }
    }

    public class Issue1874BytesHolder
    {
        public byte[] Bytes { get; set; }
    }

    public class Issue1874OutputBytesType : ObjectGraphType<Issue1874BytesHolder>
    {
        public Issue1874OutputBytesType()
        {
            Field(x => x.Bytes);
        }
    }

    public class Issue1874InputBytesType : InputObjectGraphType<Issue1874BytesHolder>
    {
        public Issue1874InputBytesType()
        {
            Field(x => x.Bytes);
        }
    }
}
