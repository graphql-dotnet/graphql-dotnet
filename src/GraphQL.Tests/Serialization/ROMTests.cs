using GraphQLParser;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Serialization
{
    public class ROMTests
    {
        public class Person
        {
            public ROM Name { get; set; }
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_ROM(IGraphQLTextSerializer serializer)
        {
            var test = "{\"name\": \"abc\"}";
            var actual = serializer.Deserialize<Person>(test);
            actual.Name.ShouldBe("abc");
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_ROM(IGraphQLTextSerializer serializer)
        {
            var person = new Person
            {
                Name = "abc"
            };
            var actual = serializer.Serialize(person);
            actual.ShouldBeCrossPlatJson("{\"Name\": \"abc\"}");
        }
    }
}
