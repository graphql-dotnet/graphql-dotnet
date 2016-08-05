using GraphQL.Types;

namespace GraphQL.Tests.Execution
{
    public class EnumAsInputsTests : QueryTestBase<EnumMutationSchema>
    {
        [Fact]
        public void mutation_input()
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
                    profileImage
                  }
                }
                ",
                @"{
                'createUser': {
                  'id': 1,
                  'gender': 'Female',
                  'profileImage': 'myimage.png'
                 }
                }");
        }

        [Fact]
        public void mutation_input_from_variables()
        {
            var inputs = @"{ 'userInput': { 'profileImage': 'myimage.png', 'gender': 'Female' } }".ToInputs();

            AssertQuerySuccess(
                @"
                mutation createUser($userInput: UserInput!) {
                  createUser(userInput: $userInput){
                    id
                    gender
                    profileImage
                  }
                }
                ",
                @"{
                'createUser': {
                  'id': 1,
                  'gender': 'Female',
                  'profileImage': 'myimage.png'
                 }
                }", inputs);
        }

        [Fact]
        public void query_can_get_enum_argument()
        {
            AssertQuerySuccess(
                @"{ user { id, gender, printGender(g: Male) }}",
                @"{
                  'user': {
                    'id': 1,
                    'gender': 'Male',
                    'printGender': 'gender: Male'
                  }
                }");
        }

        [Fact]
        public void query_can_get_long_variable()
        {
            var inputs = "{ 'userId': 1000000000000000001 }".ToInputs();

            AssertQuerySuccess(
                @"query aQuery($userId: Int!) { getLongUser(userId: $userId) { idLong }}",
                @"{
                  'getLongUser': {
                    'idLong': 1000000000000000001
                  }
                }", inputs);
        }

        [Fact]
        public void query_can_get_long_inline()
        {
            AssertQuerySuccess(
                @"query aQuery { getLongUser(userId: 1000000000000000001) { idLong }}",
                @"{
                  'getLongUser': {
                    'idLong': 1000000000000000001
                  }
                }");
        }

        [Fact]
        public void query_can_get_int_variable()
        {
            var inputs = "{ 'userId': 3 }".ToInputs();

            AssertQuerySuccess(
                @"query aQuery($userId: Int!) { getIntUser(userId: $userId) { id }}",
                @"{
                  'getIntUser': {
                    'id': 3
                  }
                }", inputs);
        }
    }

    public class EnumMutationSchema : Schema
    {
        public EnumMutationSchema()
        {
            Query = new UserQuery();
            Mutation = new MutationRoot();
        }
    }

    public class UserQuery : ObjectGraphType
    {
        public UserQuery()
        {
            Field<UserType>(
                "user",
                resolve: c => new User
                {
                    Id = 1,
                    Gender = Gender.Male,
                    ProfileImage = "hello.png"
                });
            Field<UserType>("getIntUser", "get user api",
                new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "userId",
                        Description = "user id"
                    }
                ),
                context =>
                {
                    var id = context.Argument<int>("userId");
                    return new User
                    {
                        Id = id
                    };
                }
            );

            Field<UserType>("getLongUser", "get user api",
                new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "userId",
                        Description = "user id"
                    }
                ),
                context =>
                {
                    var id = context.Argument<long>("userId");
                    return new User
                    {
                        IdLong = id
                    };
                }
            );
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
            AddValue("NotSpecified", "NotSpecified gender.", Gender.NotSpecified);
            AddValue("Male", "gender Male", Gender.Male);
            AddValue("Female", "gender female", Gender.Female);
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
                    new QueryArgument<NonNullGraphType<UserInputType>>
                    {
                        Name = "userInput",
                        Description = "user info details"
                    }
                ),
                context =>
                {
                    var input = context.Argument<CreateUser>("userInput");
                    return new User
                    {
                        Id = 1,
                        ProfileImage = input.ProfileImage,
                        Gender = input.Gender
                    };
                });
        }
    }

    public class UserType : ObjectGraphType
    {
        public UserType()
        {
            Name = "User";
            Field<IntGraphType>("id");
            Field<IntGraphType>("idLong");
            Field<StringGraphType>("profileImage");
            Field<GenderEnum>("gender");
            Field<StringGraphType>(
                "printGender",
                arguments: new QueryArguments(new QueryArgument<GenderEnum> {Name = "g"}),
                resolve: c =>
                {
                    var gender = c.Argument<Gender>("g");
                    return $"gender: {gender}";
                });
        }
    }

    public class User
    {
        public int Id { get; set; }
        public long IdLong { get; set; }
        public string ProfileImage { get; set; }
        public Gender Gender { get; set; }
    }

    public class CreateUser
    {
        public string ProfileImage { get; set; }
        public Gender Gender { get; set; }
    }

    public enum Gender
    {
        NotSpecified,
        Male,
        Female
    }
}
