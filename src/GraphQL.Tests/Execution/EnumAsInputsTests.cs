using GraphQL.Types;

namespace GraphQL.Tests.Execution
{
    public class EnumAsInputsTests : QueryTestBase<EnumMutationSchema>
    {
        [Test]
        public void something()
        {
            AssertQuerySuccess(
                @"
mutation createUser {
  createUser(userInput:{
    profileImage:""myimage.png"",
    gender: Female
  }){
    id
    gender
  }
}
",
                @"{}");
        }
    }

    public class EnumMutationSchema : Schema
    {
        public EnumMutationSchema()
        {
            Mutation = new MutationRoot();
        }
    }

    public class UserInputType : InputObjectGraphType
    {
        public UserInputType()
        {
            Name = "UserInput";
            Description = "User information for user creation";
            Field<StringGraphType>("profileImage", "profileImage of user.");
            Field<GenderEnum>("gender", "user gender.");
        }
    }

    public class GenderEnum : EnumerationGraphType
    {
        public GenderEnum()
        {
            Name = "Gender";
            Description = "User gender";
            AddValue("NotSpecified", "NotSpecified gender.", 0);
            AddValue("Male", "gender Male", 1);
            AddValue("Female", "gender female", 2);
        }
    }

    public class MutationRoot : ObjectGraphType
    {
        public MutationRoot()
        {
            Name = "MutationRoot";
            Description = "GraphQL MutationRoot for supporting create, update, delete or perform custom actions";

            Field<UserType>("createUser", "create user api",
                new QueryArguments(
                    new QueryArgument[]
                    {
                        new QueryArgument<NonNullGraphType<UserInputType>>
                        {
                            Name = "userInput",
                            Description = "user info details"
                        }
                    }
                ),
                context =>
                {
                    return new User();
                });
        }
    }

    public class UserType : ObjectGraphType
    {
        public UserType()
        {
            Name = "User";
            Field<IntGraphType>("id");
            Field<GenderEnum>("gender");
        }
    }

    public class User
    {
        public int Id { get; set; }
        public Gender Gender { get; set; }
    }

    public enum Gender
    {
        Female,
        Male
    }
}
