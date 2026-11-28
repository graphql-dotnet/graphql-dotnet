using GraphQL.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Types;

// A type declared with the C# 'closed' modifier is implicitly abstract and records its derived types, which
// is what a GraphQL interface describes. Without this a closed base registers as a plain object graph type
// and the hierarchy collapses to the members of the base. See CompilerMarkers.cs for the marker types.
public class ClosedTypeHierarchyTests
{
    [Fact]
    public void MapsToGraphQLInterface()
    {
        var schema = BuildSchema<PaymentQuery>();

        schema.Print().ShouldBeCrossPlat("""
            schema {
              query: PaymentQuery
            }

            type PaymentQuery {
              event: PaymentEvent!
              events: [PaymentEvent!]!
            }

            interface PaymentEvent {
              paymentId: String!
            }

            type PaymentAuthorized implements PaymentEvent {
              amount: Decimal!
              paymentId: String!
            }

            scalar Decimal

            type PaymentCaptured implements PaymentEvent {
              reference: String!
              paymentId: String!
            }

            """);
    }

    // this is the behaviour that was previously unreachable: the members declared by the leaf rather than
    // only those the base declares
    [Fact]
    public async Task ResolvesMembersOfTheLeaf()
    {
        var result = await ExecuteAsync<PaymentQuery>("{ event { __typename paymentId ... on PaymentAuthorized { amount } } }");

        result.ShouldBe("""{"data":{"event":{"__typename":"PaymentAuthorized","paymentId":"p-123","amount":42.5}}}""");
    }

    [Fact]
    public async Task ResolvesEachItemOfAList()
    {
        var result = await ExecuteAsync<PaymentQuery>("{ events { __typename ... on PaymentAuthorized { amount } ... on PaymentCaptured { reference } } }");

        result.ShouldBe("""{"data":{"events":[{"__typename":"PaymentAuthorized","amount":1},{"__typename":"PaymentCaptured","reference":"r-2"}]}}""");
    }

    // a closed type nested within another is an interface implementing an interface, and only the terminal
    // leaf is a possible type of either
    [Fact]
    public void NestedHierarchyImplementsEveryClosedAncestor()
    {
        var schema = BuildSchema<NodeQuery>();

        schema.Print().ShouldBeCrossPlat("""
            schema {
              query: NodeQuery
            }

            type NodeQuery {
              node: Node!
            }

            interface Node {
              id: String!
            }

            type NamedLeaf implements Branch & Node {
              name: String!
              id: String!
            }

            interface Branch implements Node {
              id: String!
            }

            """);
    }

    // inference stops at a type that is not itself closed, so OpenBranch is the terminal type and OpenLeaf
    // below it is selected as OpenBranch rather than appearing in the schema
    [Fact]
    public async Task ATypeThatIsNotClosedIsTerminal()
    {
        var schema = BuildSchema<RootQuery>();
        schema.AllTypes.Select(_ => _.Name).ShouldContain("OpenBranch");
        schema.AllTypes.Select(_ => _.Name).ShouldNotContain("OpenLeaf");

        var result = await ExecuteAsync<RootQuery>("{ root { __typename } }");

        result.ShouldBe("""{"data":{"root":{"__typename":"OpenBranch"}}}""");
    }

    // an ordinary abstract class carries no derived type list, so it keeps mapping to an object graph type
    [Fact]
    public void PlainAbstractBaseIsUnchanged()
    {
        var schema = BuildSchema<PlainQuery>();

        schema.AllTypes["PlainBase"].ShouldBeAssignableTo<IObjectGraphType>();
    }

    [Fact]
    public void TerminalTypesExpandThroughNestedHierarchies()
    {
        ClosedTypeInfo.Find(typeof(Node))!.TerminalDerivedTypes.ShouldBe([typeof(NamedLeaf)]);
        ClosedTypeInfo.Find(typeof(Branch))!.TerminalDerivedTypes.ShouldBe([typeof(NamedLeaf)]);
        ClosedTypeInfo.GetClosedBaseTypes(typeof(NamedLeaf)).ShouldBe([typeof(Branch), typeof(Node)]);
    }

    // the marker is matched by full name, so an unrelated attribute of the same simple name is not a closed
    // type and the hierarchy below it is left alone
    [Fact]
    public void DecoyAttributeIsNotAClosedType()
    {
        ClosedTypeInfo.IsClosedType(typeof(DecoyBase)).ShouldBeFalse();

        BuildSchema<DecoyQuery>().AllTypes["DecoyBase"].ShouldBeAssignableTo<IObjectGraphType>();
    }

    // pins the metadata the library matches on; it cannot reference the attribute, since it targets
    // frameworks that predate it
    [Fact]
    public void ClosedTypeIsMarkedWithTheExpectedAttribute()
    {
        typeof(PaymentEvent).GetCustomAttributesData()
            .Select(_ => _.AttributeType.FullName)
            .ShouldContain("System.Runtime.CompilerServices.IsClosedTypeAttribute");

        typeof(PaymentEvent).IsAbstract.ShouldBeTrue();
    }

    private static ISchema BuildSchema<TQuery>()
        where TQuery : class
    {
        var services = new ServiceCollection();
        services.AddGraphQL(_ => _.AddAutoSchema<TQuery>().AddSystemTextJson());
        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        return schema;
    }

    private static async Task<string> ExecuteAsync<TQuery>(string query)
        where TQuery : class
    {
        var services = new ServiceCollection();
        services.AddGraphQL(_ => _.AddAutoSchema<TQuery>().AddSystemTextJson());
        var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<IDocumentExecuter>().ExecuteAsync(_ =>
        {
            _.RequestServices = provider;
            _.Schema = provider.GetRequiredService<ISchema>();
            _.Query = query;
        });

        return provider.GetRequiredService<IGraphQLTextSerializer>().Serialize(result);
    }

    private class PaymentQuery
    {
        public static PaymentEvent Event => new PaymentAuthorized("p-123", 42.5m);

        public static List<PaymentEvent> Events => [new PaymentAuthorized("p-1", 1m), new PaymentCaptured("p-2", "r-2")];
    }

    private class NodeQuery
    {
        public static Node Node => new NamedLeaf("n-1", "the name");
    }

    private class RootQuery
    {
        public static Root Root => new OpenLeaf("r-1");
    }

    private class PlainQuery
    {
        public static PlainBase Value => new PlainDerived("p-1");
    }

    private class DecoyQuery
    {
        public static DecoyBase Value => new DecoyDerived();
    }

    public closed record PaymentEvent(string PaymentId);

    public sealed record PaymentAuthorized(string PaymentId, decimal Amount) : PaymentEvent(PaymentId);

    public sealed record PaymentCaptured(string PaymentId, string Reference) : PaymentEvent(PaymentId);

    public closed record Node(string Id);

    public closed record Branch(string Id) : Node(Id);

    public sealed record NamedLeaf(string Id, string Name) : Branch(Id);

    public closed record Root(string Id);

    public record OpenBranch(string Id) : Root(Id);

    public sealed record OpenLeaf(string Id) : OpenBranch(Id);

    public abstract record PlainBase(string Id);

    public sealed record PlainDerived(string Id) : PlainBase(Id);

    [IsClosedType(DerivedTypes = [typeof(DecoyDerived)])]
    public record DecoyBase(string Id);

    public sealed record DecoyDerived() : DecoyBase("d-1");

    // the same simple name as the compiler marker but a different namespace, so it must not be treated as a
    // closed type
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class IsClosedTypeAttribute : Attribute
    {
        public Type[] DerivedTypes { get; set; } = [];
    }
}
