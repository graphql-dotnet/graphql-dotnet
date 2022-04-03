using System.Text;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/956
public class Bug956 : QueryTestBase<Bug956Schema>
{
    [Fact]
    public void base64_should_Convert_to_binary()
    {
        var query = @"{ get(base64: ""R3JhcGhRTCE="") }";
        AssertQuerySuccess(query, @"{""get"": ""R3JhcGhRTCE=""}");
    }
}

public sealed class Bug956QueryType : ObjectGraphType
{
    public Bug956QueryType()
    {
        Field<StringGraphType>(
           "get",
           arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "base64" }),
           resolve: ctx =>
           {
               var asString = ctx.GetArgument<string>("base64");
               asString.ShouldBe("R3JhcGhRTCE="); // GraphQL!

               var asList = ctx.GetArgument<List<byte>>("base64");
               asList.Count.ShouldBe(12);

               var asBinary = ctx.GetArgument<byte[]>("base64");
               Encoding.UTF8.GetString(asBinary).ShouldBe("GraphQL!");

               return asString;
           });
    }
}

public class Bug956Schema : Schema
{
    public Bug956Schema()
    {
        Query = new Bug956QueryType();
    }
}
