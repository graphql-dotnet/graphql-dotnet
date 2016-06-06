using GraphQL.Types;
using GraphQL2;
using Should;

namespace GraphQL.Tests
{
    public class Class1
    {
        [Test]
        public async void something()
        {
            var schema = new GraphQL2.StarWarsSchema(new GraphQL2.StarWarsData());
            var engine = GraphQLEngine.For(schema);
            var query = @"query aQuery { hero { id } }";

            var result = await engine.ExecuteAsync(query);

            result.Errors.ShouldBeNull();
        }

        [Test]
        public void something2()
        {
            var schema = new GraphQL2.StarWarsSchema(new GraphQL2.StarWarsData());
            var wrapper = new GraphQLSchemaWrapper(schema);
            var type = wrapper.TypeFor("human") as GraphQLObjectType;
            var nameField = type.FieldFor("name");

            var human = new GraphQL2.Human {Name = "Luke"};
            var context = new ResolveFieldContext
            {
                Source = human
            };

            var result = nameField.Resolver.Resolve(context);

            result.ShouldNotBeNull();
            result.ShouldEqual("Luke");
        }
    }
}
