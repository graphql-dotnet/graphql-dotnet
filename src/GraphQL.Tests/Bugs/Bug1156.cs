using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug1156 : QueryTestBase<Bug1156Schema>
    {
        [Fact]
        public void f()
        {
            var query = @"
{
    type1 {
        field1A
        field1B
    }

    type2 {
        field2A
        field2B
    }
}";
            var result = AssertQueryWithErrors(query, null, expectedErrorCount: 1);
            result.Errors[0].Message.ShouldBe(@"You are trying to register a second GraphType 'GraphQL.Tests.Bugs.Type2' with the name 'MyType'.
A GraphType 'GraphQL.Tests.Bugs.Type1' with the name 'MyType' already exists. Make sure your schema does not contain
different graph types with the same name since all schema types must have unique names as per specification.");
        }
    }

    public sealed class Type1 : ObjectGraphType
    {
        public Type1()
        {
            Name = "MyType";

            Field<StringGraphType>("Field1A", resolve: _ => "Field1A Value");
            Field<StringGraphType>("Field1B", resolve: _ => "Field1B Value");
        }
    }

    public sealed class Type2 : ObjectGraphType
    {
        public Type2()
        {
            Name = "MyType";

            Field<StringGraphType>("Field2A", resolve: _ => "Field2A Value");
            Field<StringGraphType>("Field2B", resolve: _ => "Field1B Value");
        }
    }

    public sealed class QueryType : ObjectGraphType
    {
        public QueryType()
        {
            Field<Type1>().Name("type1").Resolve(x => new { });
            Field<Type2>().Name("type2").Resolve(x => new { });
        }
    }

    public class Bug1156Schema : Schema
    {
        public Bug1156Schema()
        {
            Query = new QueryType();
        }
    }
}
