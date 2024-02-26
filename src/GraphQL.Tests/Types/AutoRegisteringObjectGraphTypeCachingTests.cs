using System.ComponentModel;
using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class AutoRegisteringObjectGraphTypeCachingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CachingWorks(bool withCaching)
    {
        TestGraphType.Configured = false;
        GlobalSwitches.EnableReflectionCaching = withCaching;
        try
        {
            var graph1 = new TestGraphType();
            await ValidateAsync(graph1);

            if (!withCaching)
            {
                Should.Throw<AlreadyConfiguredException>(() => new TestGraphType());
                TestGraphType.Configured = false;
            }
            var graph2 = new TestGraphType();
            await ValidateAsync(graph2);

            async Task ValidateAsync(TestGraphType graph)
            {
                graph.Name.ShouldBe("Class1");
                graph.Description.ShouldBe("Desc1");
                graph.DeprecationReason.ShouldBe("Dep1");
                graph.Metadata.Count.ShouldBe(2); // deprecation directive and Test metadata
                graph.GetMetadata<string>("Test").ShouldBe("Value");
                graph.Fields.Count.ShouldBe(3);

                var field = graph.GetField("Id").ShouldNotBeNull();
                field.Name.ShouldBe("Id");
                field.Type.ShouldBe(typeof(NonNullGraphType<IdGraphType>));
                field.ResolvedType = new NonNullGraphType(new IdGraphType()); // simulate initialization
                field.Description.ShouldBeNull();
                field.DeprecationReason.ShouldBeNull();
                field.Metadata.Count.ShouldBe(0);
                var ret = await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
                {
                    Source = new Class1(),
                    FieldDefinition = field,
                });
                ret.ShouldBeOfType<int>().ShouldBe(5);

                field = graph.GetField("Print").ShouldNotBeNull();
                field.Name.ShouldBe("Print");
                field.Type.ShouldBe(typeof(GraphQLClrOutputTypeReference<string>));
                field.ResolvedType = new StringGraphType(); // simulate initialization
                field.Description.ShouldBe("Desc2");
                field.DeprecationReason.ShouldBeNull();
                field.Metadata.Count.ShouldBe(1);
                field.GetMetadata<string>("Test2").ShouldBe("Value2");
                field.Arguments.ShouldNotBeNull().Count.ShouldBe(1);
                var arg = field.Arguments[0];
                arg.Name.ShouldBe("id");
                arg.Type.ShouldBe(typeof(NonNullGraphType<IdGraphType>));
                arg.ResolvedType = new NonNullGraphType(new IdGraphType()); // simulate initialization
                arg.Description.ShouldBe("IdDesc");
                arg.DeprecationReason.ShouldBeNull();
                arg.Metadata.Count.ShouldBe(0);
                var resolveContext = new ResolveFieldContext
                {
                    Source = new Class1(),
                    Arguments = new Dictionary<string, ArgumentValue> { { "id", new(10, ArgumentSource.Literal) } },
                    FieldDefinition = field,
                };
                var printRet = await field.Resolver.ShouldNotBeNull().ResolveAsync(resolveContext);
                printRet.ShouldBeOfType<string>().ShouldBe("10");

                field = graph.GetField("Value").ShouldNotBeNull();
                field.Name.ShouldBe("Value");
                field.Type.ShouldBe(typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>));
                field.Description.ShouldBeNull();
                field.DeprecationReason.ShouldBe("Dep2");
                field.Metadata.Count.ShouldBe(1); // deprecation directive

                var valueRet = await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext { Source = new Class1() });
                valueRet.ShouldBeOfType<int>().ShouldBe(3);
            }
        }
        finally
        {
            GlobalSwitches.EnableReflectionCaching = false;
            TestGraphType.Configured = false;
        }
    }

    private class TestGraphType : AutoRegisteringObjectGraphType<Class1>
    {
        public static bool Configured { get; set; }

        protected override void ConfigureGraph()
        {
            base.ConfigureGraph();
            if (Configured)
                throw new AlreadyConfiguredException();
            Configured = true;
        }
    }

    private class AlreadyConfiguredException : Exception { }

    [Metadata("Test", "Value")]
    [Description("Desc1")]
    [Obsolete("Dep1")]
    private class Class1
    {
        [Id]
        public int Id { get; set; } = 5;
        [Metadata("Test2", "Value2")]
        [Description("Desc2")]
        public string? Print([Id, Description("IdDesc")] int id) => id.ToString();
        [Obsolete("Dep2")]
        public int Value => 3;
    }
}
