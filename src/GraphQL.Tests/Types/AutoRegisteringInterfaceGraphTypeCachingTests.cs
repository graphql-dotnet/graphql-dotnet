using System.ComponentModel;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class AutoRegisteringInterfaceGraphTypeCachingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CachingWorks(bool withCaching)
    {
        TestGraphType.Configured = false;
        GlobalSwitches.EnableReflectionCaching = withCaching;
        try
        {
            var graph1 = new TestGraphType();
            Validate(graph1);

            if (!withCaching)
            {
                Should.Throw<AlreadyConfiguredException>(() => new TestGraphType());
                TestGraphType.Configured = false;
            }
            var graph2 = new TestGraphType();
            Validate(graph2);

            void Validate(TestGraphType graph)
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
                field.Description.ShouldBeNull();
                field.DeprecationReason.ShouldBeNull();
                field.Metadata.Count.ShouldBe(0);
                field.Resolver.ShouldBeNull();

                field = graph.GetField("Print").ShouldNotBeNull();
                field.Name.ShouldBe("Print");
                field.Type.ShouldBe(typeof(GraphQLClrOutputTypeReference<string>));
                field.Description.ShouldBe("Desc2");
                field.DeprecationReason.ShouldBeNull();
                field.Metadata.Count.ShouldBe(1);
                field.GetMetadata<string>("Test2").ShouldBe("Value2");
                field.Arguments.ShouldNotBeNull().Count.ShouldBe(1);
                var arg = field.Arguments[0];
                arg.Name.ShouldBe("id");
                arg.Type.ShouldBe(typeof(NonNullGraphType<IdGraphType>));
                arg.Description.ShouldBe("IdDesc");
                arg.DeprecationReason.ShouldBeNull();
                arg.Metadata.Count.ShouldBe(0);
                field.Resolver.ShouldBeNull();

                field = graph.GetField("Value").ShouldNotBeNull();
                field.Name.ShouldBe("Value");
                field.Type.ShouldBe(typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>));
                field.Description.ShouldBeNull();
                field.DeprecationReason.ShouldBe("Dep2");
                field.Metadata.Count.ShouldBe(1); // deprecation directive
                field.Resolver.ShouldBeNull();
            }
        }
        finally
        {
            GlobalSwitches.EnableReflectionCaching = false;
            TestGraphType.Configured = false;
        }
    }

    private class TestGraphType : AutoRegisteringInterfaceGraphType<Class1>
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
