using GraphQL.SystemTextJson;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    [Collection("Sequential_UseDeclaringTypeNames")]
    public class Bug_nested_type_names_dont_conflict :
        QueryTestBase<Bug_nested_type_names_dont_conflict.MutationSchema>
    {
        [Fact]
        public void nested_type_names_dont_conflict()
        {
            GlobalSwitches.UseDeclaringTypeNames = true;

            try
            {
                var inputs = @"{ ""input_0"": { ""id"": ""123""} }".ToInputs();
                var query = @"
mutation M($input_0: Bug_nested_type_names_dont_conflict_MyInputClass_MyInput!) {
  run(input: $input_0)
}
";
                var expected = @"{ ""run"": ""123"" }";
                AssertQuerySuccess(query, expected, inputs);
            }
            finally
            {
                GlobalSwitches.UseDeclaringTypeNames = false;
            }
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
                    arguments: new QueryArguments(new QueryArgument<MyInputClass.MyInput> { Name = "input" }),
                    resolve: ctx => ctx.GetArgument<MyInputClass>("input").Id);
                Field<StringGraphType>(
                    "run2",
                    arguments: new QueryArguments(new QueryArgument<MyInputClass2.MyInput> { Name = "input" }),
                    resolve: ctx => ctx.GetArgument<MyInputClass2>("input").Id);
            }
        }

        public class MyInputClass
        {
            public string Id { get; set; }

            public class MyInput : InputObjectGraphType
            {
                public MyInput()
                {
                    Field<NonNullGraphType<StringGraphType>>("id");
                }
            }
        }
        public class MyInputClass2
        {
            public string Id { get; set; }

            public class MyInput : InputObjectGraphType
            {
                public MyInput()
                {
                    Field<NonNullGraphType<StringGraphType>>("id");
                }
            }
        }
    }
}
