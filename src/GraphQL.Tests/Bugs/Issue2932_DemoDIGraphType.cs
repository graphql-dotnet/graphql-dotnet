#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using GraphQL.DI;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Bugs;

public class Issue2932_DemoDIGraphType : QueryTestBase<Issue2932_DemoDIGraphType.TestSchema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        register.Singleton<TestSchema>();
        register.Singleton<DIGraphType<SampleGraph, SampleSource>>();
        register.Scoped<Service1>();
        register.Scoped<Service2>();
        // note: in this example, SampleGraph is not registered, but is created for every field resolver (except static methods) -- see DIGraphType.MemberInstanceFunc
    }

    [Fact]
    public void TestDIAutoRegisteringGraphType()
    {
        // tests the following functionality that can be implemented in a derived class:
        //   - members can pull from another type rather than only TSourceType
        //   - instance can pull from DI rather than context.Source
        //   - static members do not create an instance
        //   - attributes can be pulled from another class instead of TSourceType
        //
        // note: this is just an example of what can be done, and does not necessarily indicate a preferred programming pattern

        AssertQuerySuccess(
            @"
{
  example (id: 1, name: ""john doe"") {
    id
    name
    children
    service2Test
    counter1: counter
    counter2: counter
    scopedCounter1: scopedCounter
    scopedCounter2: scopedCounter
    counter3: counter
  }
}
",
            @"
{
  ""example"": {
    ""id"": ""1"",
    ""name"": ""john doe"",
    ""children"": [""Happy"",""Dopey"",""Grumpy""],
    ""service2Test"": 2,
    ""counter1"": 0,
    ""counter2"": 1,
    ""scopedCounter1"": 0,
    ""scopedCounter2"": 0,
    ""counter3"": 2
  }
}
");
    }

    [Fact]
    public void DIGraphInheritsCorrectly()
    {
        // verifies the derived type still implements ObjectGraphType<TSourceType>
        typeof(ObjectGraphType<SampleSource>).IsAssignableFrom(typeof(DIGraphType<SampleGraph, SampleSource>)).ShouldBeTrue();
    }

    public class TestSchema : Schema
    {
        public TestSchema()
        {
            Query = new QueryGraph();
        }
    }

    public class QueryGraph : ObjectGraphType
    {
        public QueryGraph()
        {
            Field<NonNullGraphType<DIGraphType<SampleGraph, SampleSource>>>("example")
                .Argument<NonNullGraphType<IntGraphType>>("id")
                .Argument<NonNullGraphType<StringGraphType>>("name")
                .Resolve(context => new SampleSource { Id = context.GetArgument<int>("id"), Name = context.GetArgument<string>("name") });
        }
    }

    public class Service1
    {
        private int _counter;

        public Task<IEnumerable<string>> GetChildrenAsync(int sourceId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IEnumerable<string>>(sourceId switch
            {
                1 => new string[] { "Happy", "Dopey", "Grumpy" },
                _ => throw new ArgumentOutOfRangeException()
            });
        }

        public int Count => _counter++;
    }

    public class Service2
    {
        public int Value => 2;
    }

    public class SampleSource
    {
        public int Id;
        public string Name = null!;
    }

    // ==== demo graph using new DIGraph<T> base class ====
    // attributes still work
    [Name("Sample")]
    public class SampleGraph : DIGraph<SampleSource>
    {
        // standard DI injection of services
        private readonly Service1 _service1;
        public SampleGraph(Service1 service1)
        {
            _service1 = service1;
        }

        // methods work with attributes and can use source property
        [Id]
        public int Id() => Source.Id;

        // can write as static methods so that an instance of SampleGraph is not constructed at runtime
        // static methods can pull source from an attribute or similar
        public static string Name([FromSource] SampleSource source) => source.Name; // do not create instance of SampleGraph

        // sample of using an asynchronous method with a cancellation token provided through the class instead of within the method
        // also demonstrates auto inference of return type and removal of "Async" from asynchronous methods
        public Task<IEnumerable<string>> ChildrenAsync() => _service1.GetChildrenAsync(Source.Id, RequestAborted);

        // sample of a static method, pulling in a specific service
        public static int Service2Test([FromServices] Service2 service2) => service2.Value;

        // sample of returning data from a scoped service
        public int Counter() => _service1.Count;

        // sample of the field resolver creating a scope for this method, and returning data from a scoped service within that scope
        [Scoped]
        public int ScopedCounter() => _service1.Count;
    }

    public class DIGraphType<DIObject, TSourceType> : AutoRegisteringObjectGraphType<TSourceType>
        where DIObject : DIGraph<TSourceType>
    {
        protected override void ConfigureGraph()
        {
            // do not configure attributes set on TSourceType
            // base.ConfigureGraph();

            // configure attributes set on DIObject instead
            // todo: AutoRegisteringHelper.ApplyGraphQLAttributes should be public for this sample to work as written
            AutoRegisteringHelper.ApplyGraphQLAttributes<DIObject>(this);
        }

        // only process methods declared directly on DIObject -- not anything declared on TSourceType
        protected override IEnumerable<MemberInfo> GetRegisteredMembers()
        {
            return typeof(DIObject)
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        // each field resolver will build a new instance of DIObject
        protected override LambdaExpression BuildMemberInstanceExpression(MemberInfo memberInfo)
            => (IResolveFieldContext context) => MemberInstanceFunc(context);

        private DIObject MemberInstanceFunc(IResolveFieldContext context)
        {
            // create a new instance of DIObject, filling in any constructor arguments from DI
            var graph = ActivatorUtilities.CreateInstance<DIObject>(context.RequestServices ?? throw new MissingRequestServicesException());
            // set the context
            graph.Context = context;
            // return the object
            return graph;
        }
    }

    public class DIGraph<TSourceType>
    {
        public IResolveFieldContext Context { get; internal set; } = null!;
        public TSourceType Source => (TSourceType)Context.Source!;
        public CancellationToken RequestAborted => Context.CancellationToken;
    }
}
