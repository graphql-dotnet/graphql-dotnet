using GraphQL.Resolvers;
using GraphQLParser.AST;

namespace GraphQL.Tests
{
    public class NameFieldResolverTests
    {
        [Theory]
        [InlineData("age", 20)]
        [InlineData("AGE", 20)]
        [InlineData("Name", "Anyone")]
        [InlineData("naMe", "Anyone")]
        [InlineData("FullInfo", "Anyone 20")]
        [InlineData("fullInfo", "Anyone 20")]
        [InlineData(null, null)]
        [InlineData("unknown", "", true)]
        [InlineData("FullInfoWithParam", "", true)]
        public void resolve_should_work_with_properties_and_methods(string name, object expected, bool throws = false)
        {
            var person = new Person
            {
                Age = 20,
                Name = "Anyone"
            };

            Func<object> result = () => NameFieldResolver.Instance.Resolve(new ResolveFieldContext { Source = person, FieldDefinition = new GraphQL.Types.FieldType { Name = name }, FieldAst = new GraphQLField { Name = name == null ? default : new GraphQLName(name) } });

            if (throws)
                Should.Throw<InvalidOperationException>(() => result());
            else
                result().ShouldBe(expected);
        }

        public class Person
        {
            public int Age { get; set; }

            public string Name { get; set; }

            public string FullInfo() => Name + " " + Age;

            public string FullInfoWithParam(string prefix) => prefix + FullInfo();
        }
    }
}
