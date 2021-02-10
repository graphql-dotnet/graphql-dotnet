using GraphQL.SystemTextJson;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug11874 : QueryTestBase<Bug11874Schema>
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

    public class Bug11874Schema : Schema
    {
        public Bug11874Schema()
        {
            Query = new DemoQuery();
        }
    }

    public class DemoQuery : ObjectGraphType
    {
        public DemoQuery()
        {
            Field<OutputBytesType>(
                "bytes",
                arguments: new QueryArguments(new QueryArgument<InputBytesType> { Name = "bytesObject" }),
                resolve: context =>
                {
                    var bytesObject = context.GetArgument<BytesHolder>("bytesObject");
                    return bytesObject;
                }
            );
        }
    }

    public class BytesHolder
    {
        public byte[] Bytes { get; set; }
    }

    public class OutputBytesType : ObjectGraphType<BytesHolder>
    {
        public OutputBytesType()
        {
            Field(x => x.Bytes);
        }
    }

    public class InputBytesType : InputObjectGraphType<BytesHolder>
    {
        public InputBytesType()
        {
            Field(x => x.Bytes);
        }
    }
}
