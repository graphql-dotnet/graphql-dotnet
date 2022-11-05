using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

[Collection("StaticTests")]
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
            Field<StringGraphType>("run")
                .Argument<MyInputClass.MyInput>("input")
                .Resolve(ctx => ctx.GetArgument<MyInputClass>("input").Id);
            Field<StringGraphType>("run2")
                .Argument<MyInputClass2.MyInput>("input")
                .Resolve(ctx => ctx.GetArgument<MyInputClass2>("input").Id);
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
