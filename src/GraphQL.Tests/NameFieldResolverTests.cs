using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQLParser.AST;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests;

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
    [InlineData("FullInfoWithParam", "test Anyone 20")]
    [InlineData("FULLINFOWITHPARAM", "test Anyone 20")]
    [InlineData("FullInfoWithContext", "Anyone 20")]
    [InlineData("FromService", "hello")]
    [InlineData("AmbiguousExample", "", true)]
    [InlineData("ShadowedName", "Anyone")]
    [InlineData("BaseName", "Base")]
    [InlineData("BaseMethod", "Base2")]
    public async Task resolve_should_work_with_properties_and_methods(string name, object expected, bool throws = false)
    {
        var person = new Person
        {
            Age = 20,
            Name = "Anyone"
        };

        var services = new ServiceCollection();
        services.AddSingleton(new Class1());
        Func<ValueTask<object>> result = () => NameFieldResolver.Instance.ResolveAsync(
            new ResolveFieldContext
            {
                Source = person,
                FieldDefinition = new GraphQL.Types.FieldType { Name = name },
                FieldAst = new GraphQLField { Name = name == null ? default : new GraphQLName(name) },
                Arguments = new Dictionary<string, ArgumentValue>()
                {
                    { "prefix", new ArgumentValue("test ", ArgumentSource.Literal) }
                },
                RequestServices = services.BuildServiceProvider(),
            });

        if (throws)
            await Should.ThrowAsync<InvalidOperationException>(async () => await result().ConfigureAwait(false)).ConfigureAwait(false);
        else
            (await result().ConfigureAwait(false)).ShouldBe(expected);
    }

    public class PersonBase
    {
        public string ShadowedName { get; } = "n/a";

        public string BaseName { get; } = "Base";

        public string BaseMethod() => "Base2";
    }

    public class Person : PersonBase
    {
        public int Age { get; set; }

        public string Name { get; set; }

        public string FullInfo() => Name + " " + Age;

        public string FullInfoWithParam(string prefix) => prefix + FullInfo();

        public string FullInfoWithContext(IResolveFieldContext context) => ((Person)context.Source).FullInfo();

        public string FromService([FromServices] Class1 obj) => obj.Value;

        public string AmbiguousExample() => "";

        public string AmbiguousExample(string ret) => ret;

        public new string ShadowedName => Name;
    }

    public class Class1
    {
        public string Value { get; } = "hello";
    }
}
