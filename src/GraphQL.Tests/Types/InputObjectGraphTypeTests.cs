#nullable enable

using GraphQL.Tests.Subscription;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class InputObjectGraphTypeTests
{
    [Fact]
    public void should_throw_an_exception_if_input_object_graph_type_contains_object_graph_type_field()
    {
        var type = new InputObjectGraphType();
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<ObjectGraphType>("test"));

        exception.Message.ShouldContain("Input type 'InputObject' can have fields only of input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType.");
    }

    [Fact]
    public void should_throw_an_exception_if_object_graph_type_contains_Input_object_graph_type_field()
    {
        var type = new ObjectGraphType();
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<InputObjectGraphType>("test"));
        exception.Message.ShouldContain("Output type 'Object' can have fields only of output types: ScalarGraphType, ObjectGraphType, InterfaceGraphType, UnionGraphType or EnumerationGraphType.");
    }

    [Fact]
    public async Task input_resolver_works()
    {
        // demonstrates having a StringGraphType field that accepts a Uri as input
        // the string value is coerced to a Uri prior to beginning execution of the request
        var inputType = new InputObjectGraphType<Class1>();
        inputType.Field<StringGraphType, Uri>("url")
            .ParseValue(original =>
            {
                var originalString = (string?)original;
                if (originalString == null)
                    return null;
                return new Uri(originalString);
            });
        var queryType = new ObjectGraphType();
        queryType.Field<StringGraphType>(
            "test",
            arguments: new QueryArguments(
                new QueryArgument(inputType)
                {
                    Name = "input"
                }),
            resolve: context =>
            {
                var input = context.GetArgument<Class1>("input");
                return input.Url?.ToString();
            });
        var schema = new Schema { Query = queryType };
        // check with valid url
        var result = await new DocumentExecuter().ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = """{ test(input: { url: "http://www.google.com" }) }""";
        });
        result.ShouldBeSimilarTo("""{"data":{"test":"http://www.google.com/"}}""");
        // check with invalid url
        result = await new DocumentExecuter().ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = """{ test(input: { url: "abcd" }) }""";
        });
        result.ShouldBeSimilarTo("""{"errors":[{"message":"Invalid literal for argument \u0027input\u0027 of field \u0027test\u0027. Invalid URI: The format of the URI could not be determined.","locations":[{"line":1,"column":15}],"extensions":{"code":"INVALID_LITERAL","codes":["INVALID_LITERAL","URI_FORMAT"],"number":"5.6"}}]}""");
    }

    private class Class1
    {
        public Uri? Url { get; set; }
    }
}
