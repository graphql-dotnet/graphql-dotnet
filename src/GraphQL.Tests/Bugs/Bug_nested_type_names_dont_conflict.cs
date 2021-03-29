using GraphQL.SystemTextJson;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug_nested_type_names_dont_conflict :
        QueryTestBase<Bug_nested_type_names_dont_conflict.MutationSchema>
    {
        [Fact]
        public void nested_type_names_dont_conflict()
        {
            var inputs = @"{ ""input_0"": { ""id"": ""123""} }".ToInputs();
            var query = @"
mutation M($input_0: MyInput!) {
  run(input: $input_0)
}
";
            var expected = @"{ ""run"": ""123"" }";
            AssertQuerySuccess(query, expected, inputs);
        }

        public class MutationSchema : Schema
        {
            public MutationSchema()
            {
                Mutation = new MyMutation();
            }
        }

        public class MyMutation : ObjectGraphType
        {
            public MyMutation()
            {
                Field<StringGraphType>(
                    "run",
                    arguments: new QueryArguments(new QueryArgument<MyInput> {Name = "input"}),
                    resolve: ctx => ctx.GetArgument<MyInputClass>("input").Id);
            }
        }

        public class MyInputClass
        {
            public string Id { get; set; }
        }

        public class MyInput : InputObjectGraphType
        {
            public MyInput()
            {
                Name = "MyInput"; // changed from "MyInput "
                Field<NonNullGraphType<StringGraphType>>("id");
            }
        }
    }
}
