using GraphQL.SystemTextJson;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Issue1874 : QueryTestBase<Bug1874Schema>
    {
        [Fact]
        public void duplicated_type_names_should_throw_error()
        {
            var query = @"
                query BytesRequest($bytesHolder: InputBytesType) {
                    bytes(bytesObject: $bytesHolder) {
                        bytes
                    }
                }";

            AssertQuerySuccess(query, @"{ ""bytes"": { ""bytes"": [1, 2, 3, 4] } }", @"{ ""bytesHolder"": { ""bytes"": [1, 2, 3, 4] } }".ToInputs());
        }
    }

    public class Bug1874Schema : Schema
    {
        public Bug1874Schema()
        {
            Query = new Bug1874Query();
        }
    }

    public class Bug1874Query : ObjectGraphType
    {
        public Bug1874Query()
        {
            Field<Bug1874OutputBytesType>(
                "bytes",
                arguments: new QueryArguments(new QueryArgument<Bug1874InputBytesType> { Name = "bytesObject" }),
                resolve: context =>
                {
                    var bytesObject = context.GetArgument<Bug1874BytesHolder>("bytesObject");
                    return bytesObject;
                }
            );
        }
    }

    public class Bug1874BytesHolder
    {
        public byte[] Bytes { get; set; }
    }

    public class Bug1874OutputBytesType : ObjectGraphType<Bug1874BytesHolder>
    {
        public Bug1874OutputBytesType()
        {
            Field(x => x.Bytes);
        }
    }

    public class Bug1874InputBytesType : InputObjectGraphType<Bug1874BytesHolder>
    {
        public Bug1874InputBytesType()
        {
            Field(x => x.Bytes);
        }
    }
}
