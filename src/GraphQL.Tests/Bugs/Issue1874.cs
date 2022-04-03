using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Bugs;

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

    [Fact]
    public void string_should_work()
    {
        var query = @"
                query BytesRequest($bytesHolder: Issue1874Input64BytesType) {
                    bytes64(bytesObject: $bytesHolder) {
                        bytes
                    }
                }";

        var str1234 = System.Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        AssertQuerySuccess(query, @"{ ""bytes64"": { ""bytes"": """ + str1234 + @""" } }", (@"{ ""bytesHolder"": { ""bytes"": """ + str1234 + @""" } }").ToInputs());
    }

    [Fact]
    public void string_literal_should_work()
    {
        var str1234 = System.Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        var query = @"
                query BytesRequest {
                    bytes64(bytesObject: { bytes: """ + str1234 + @"""}) {
                        bytes
                    }
                }";

        AssertQuerySuccess(query, @"{ ""bytes64"": { ""bytes"": """ + str1234 + @""" } }");
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

        Field<Issue1874Output64BytesType>(
            "bytes64",
            arguments: new QueryArguments(new QueryArgument<Issue1874Input64BytesType> { Name = "bytesObject" }),
            resolve: context =>
            {
                var bytesObject = context.GetArgument<Issue1874BytesHolder>("bytesObject");
                bytesObject.Bytes.ShouldBe(new byte[] { 1, 2, 3, 4 });
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

public class Issue1874Output64BytesType : ObjectGraphType<Issue1874BytesHolder>
{
    public Issue1874Output64BytesType()
    {
        Field(x => x.Bytes, type: typeof(Issue1874Base64GraphType));
    }
}

public class Issue1874Input64BytesType : InputObjectGraphType<Issue1874BytesHolder>
{
    public Issue1874Input64BytesType()
    {
        Field(x => x.Bytes, type: typeof(Issue1874Base64GraphType));
    }
}

public class Issue1874Base64GraphType : ScalarGraphType
{
    public override object ParseLiteral(GraphQLValue value)
    {
        return value switch
        {
            GraphQLStringValue s => Convert.FromBase64String((string)s.Value), // string conversion for NET48
            _ => throw new NotSupportedException()
        };
    }

    public override object ParseValue(object value)
        => System.Convert.FromBase64String(value.ToString());

    public override object Serialize(object value)
        => System.Convert.ToBase64String(value is byte[] valueBytes ? valueBytes : ((System.Collections.Generic.IEnumerable<byte>)value).ToArray());
}
