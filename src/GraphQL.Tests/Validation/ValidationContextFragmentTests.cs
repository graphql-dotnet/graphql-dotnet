using GraphQL.Validation;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class ValidationContextFragmentTests
{
    [Fact]
    public void TraversesDeepSelectionSets()
    {
        // Build the AST directly so the parser's depth limit does not mask traversal failures.
        var selectionSet = new GraphQLSelectionSet([Spread("leaf")]);
        for (int i = 0; i < 20_000; ++i)
        {
            ASTNode selection = i % 2 == 0
                ? new GraphQLField(new GraphQLName("child")) { SelectionSet = selectionSet }
                : new GraphQLInlineFragment(selectionSet);
            // Pending siblings also exercise the traversal's storage and unwind order.
            selectionSet = new GraphQLSelectionSet([selection, Spread("sibling")]);
        }

        var operation = new GraphQLOperationDefinition(selectionSet);
        var leaf = Fragment("leaf", new GraphQLSelectionSet([new GraphQLField(new GraphQLName("value"))]));
        var sibling = Fragment("sibling", new GraphQLSelectionSet([new GraphQLField(new GraphQLName("value"))]));
        var context = Context(new GraphQLDocument([operation, leaf, sibling]), operation);

        context.GetRecursivelyReferencedFragments(operation, onlyUsed: true).ShouldNotBeNull().SetEquals([leaf, sibling]).ShouldBeTrue();
    }

    [Fact]
    public void TraversesDeepFragmentChainWithCycle()
    {
        const int depth = 10_000;
        var operation = new GraphQLOperationDefinition(new GraphQLSelectionSet([Spread("fragment0")]));
        var definitions = new List<ASTNode> { operation };
        var expected = new List<GraphQLFragmentDefinition>(depth);
        for (int i = 0; i < depth; ++i)
        {
            var fragment = Fragment($"fragment{i}", new GraphQLSelectionSet([Spread($"fragment{(i + 1) % depth}")]));
            definitions.Add(fragment);
            expected.Add(fragment);
        }
        var context = Context(new GraphQLDocument(definitions), operation);

        context.GetRecursivelyReferencedFragments(operation).ShouldNotBeNull().SetEquals(expected).ShouldBeTrue();
    }

    [Fact]
    public void IgnoresRepeatedCyclicAndUnknownFragments()
    {
        var document = Parser.Parse("""
            query { ...first ...second ...first ...unknown }
            fragment first on Query { child { ...shared } ...tail }
            fragment second on Query { ...shared ...last }
            fragment shared on Query { ...first ...shared ...unknown }
            fragment tail on Query { value }
            fragment last on Query { value }
            """);
        var operation = document.Operation();
        var context = Context(document, operation);

        context.GetRecursivelyReferencedFragments(operation).ShouldNotBeNull()
            .Select(x => x.FragmentName.Name.StringValue)
            .ShouldBe(["first", "shared", "tail", "second", "last"], ignoreOrder: true);
    }

    [Fact]
    public void HandlesDeeplyWrappedDirectiveVariableType()
    {
        var document = Parser.Parse("""
            query($skip: Boolean = true) { ...fragment @skip(if: $skip) }
            fragment fragment on Query { value }
            """);
        var operation = document.Operation();
        var variable = operation.Variables.ShouldNotBeNull().Items[0];
        // Invalid variable types can reach this helper before validation reports them.
        for (int i = 0; i < 20_000; ++i)
            variable.Type = new GraphQLNonNullType(new GraphQLListType(variable.Type));
        var context = Context(document, operation);

        context.GetRecursivelyReferencedFragments(operation, true).ShouldBeNull();
    }

    [Fact]
    public void PreservesShouldIncludeNodeVisitOrder()
    {
        var document = Parser.Parse("""
            query { outer { ...first } ... { ...second } }
            fragment first on Query { firstValue }
            fragment second on Query { secondValue }
            """);
        var operation = document.Operation();
        var context = new TrackingValidationContext { Document = document, Operation = operation };
        var outer = (GraphQLField)operation.SelectionSet.Selections[0];
        var inline = (GraphQLInlineFragment)operation.SelectionSet.Selections[1];
        var first = (GraphQLFragmentDefinition)document.Definitions[1];
        var second = (GraphQLFragmentDefinition)document.Definitions[2];

        context.GetRecursivelyReferencedFragments(operation, true).ShouldNotBeNull().SetEquals([first, second]).ShouldBeTrue();

        // Selection sets belonging to fragment definitions historically bypass this hook.
        context.Visited.ShouldBe([
            operation.SelectionSet,
            outer,
            outer.SelectionSet.ShouldNotBeNull(),
            outer.SelectionSet.ShouldNotBeNull().Selections[0],
            first.SelectionSet.Selections[0],
            inline,
            inline.SelectionSet,
            inline.SelectionSet.Selections[0],
            second.SelectionSet.Selections[0]
        ]);
    }

    [Fact]
    public void CanRetryAfterShouldIncludeNodeThrows()
    {
        var document = Parser.Parse("""
            query { outer { ...first ...second } }
            fragment first on Query { child { ...nested } }
            fragment nested on Query { value }
            fragment second on Query { value }
            """);
        var operation = document.Operation();
        var first = (GraphQLFragmentDefinition)document.Definitions[1];
        var context = new TrackingValidationContext
        {
            Document = document,
            Operation = operation,
            ThrowOn = first.SelectionSet.Selections[0],
        };

        Should.Throw<InvalidOperationException>(() => context.GetRecursivelyReferencedFragments(operation, true));
        context.ThrowOn = null;

        context.GetRecursivelyReferencedFragments(operation, true).ShouldNotBeNull()
            .Select(x => x.FragmentName.Name.StringValue).ShouldBe(["first", "nested", "second"], ignoreOrder: true);
    }

    [Fact]
    public void CachesSetsForAllAndUsedFragments()
    {
        var document = Parser.Parse("""
            query { ...included ...skipped @skip(if: true) }
            fragment included on Query { value }
            fragment skipped on Query { value }
            """);
        var operation = document.Operation();
        var included = (GraphQLFragmentDefinition)document.Definitions[1];
        var skipped = (GraphQLFragmentDefinition)document.Definitions[2];
        var context = Context(document, operation);

        HashSet<GraphQLFragmentDefinition> all = context.GetRecursivelyReferencedFragments(operation).ShouldNotBeNull();
        HashSet<GraphQLFragmentDefinition> used = context.GetRecursivelyReferencedFragments(operation, true).ShouldNotBeNull();
        all.SetEquals([included, skipped]).ShouldBeTrue();
        used.SetEquals([included]).ShouldBeTrue();
        context.GetRecursivelyReferencedFragments(operation).ShouldBeSameAs(all);
        context.GetRecursivelyReferencedFragments(operation, true).ShouldBeSameAs(used);
    }

    [Fact]
    public void UnionsOperationSetsWithoutChangingCachedSets()
    {
        var document = Parser.Parse("""
            query First { ...first ...shared }
            query Second { ...second ...shared }
            query Third { value }
            fragment first on Query { value }
            fragment second on Query { value }
            fragment shared on Query { value }
            """);
        var firstOperation = (GraphQLOperationDefinition)document.Definitions[0];
        var secondOperation = (GraphQLOperationDefinition)document.Definitions[1];
        var thirdOperation = (GraphQLOperationDefinition)document.Definitions[2];
        var first = (GraphQLFragmentDefinition)document.Definitions[3];
        var second = (GraphQLFragmentDefinition)document.Definitions[4];
        var shared = (GraphQLFragmentDefinition)document.Definitions[5];
        var context = Context(document, firstOperation);
        var firstSet = context.GetRecursivelyReferencedFragments(firstOperation).ShouldNotBeNull();
        var secondSet = context.GetRecursivelyReferencedFragments(secondOperation).ShouldNotBeNull();

        HashSet<GraphQLFragmentDefinition> combined = context.GetRecursivelyReferencedFragments([thirdOperation, firstOperation, secondOperation, firstOperation]).ShouldNotBeNull();
        combined.SetEquals([first, second, shared]).ShouldBeTrue();
        combined.ShouldNotBeSameAs(firstSet);
        combined.ShouldNotBeSameAs(secondSet);
        combined.Clear();

        firstSet.SetEquals([first, shared]).ShouldBeTrue();
        secondSet.SetEquals([second, shared]).ShouldBeTrue();
        context.GetRecursivelyReferencedFragments(firstOperation).ShouldBeSameAs(firstSet);
        context.GetRecursivelyReferencedFragments(secondOperation).ShouldBeSameAs(secondSet);
        context.GetRecursivelyReferencedFragments([firstOperation, secondOperation]).ShouldNotBeNull()
            .SetEquals([first, second, shared]).ShouldBeTrue();
    }

    [Fact]
    public void SingleOperationListReturnsCachedSet()
    {
        var document = Parser.Parse("""
            query { ...fragment }
            fragment fragment on Query { value }
            """);
        var operation = document.Operation();
        var context = Context(document, operation);
        var fragments = context.GetRecursivelyReferencedFragments(operation).ShouldNotBeNull();

        context.GetRecursivelyReferencedFragments([operation]).ShouldBeSameAs(fragments);
    }

    [Theory]
    [InlineData("{ value }")]
    [InlineData("{ value } fragment unused on Query { value }")]
    [InlineData("{ ...unknown } fragment unused on Query { value }")]
    public void ReturnsNullWhenNoFragmentsAreReferenced(string query)
    {
        var document = Parser.Parse(query);
        var operation = document.Operation();
        var context = Context(document, operation);

        for (int i = 0; i < 2; ++i)
        {
            context.GetRecursivelyReferencedFragments(operation).ShouldBeNull();
            context.GetRecursivelyReferencedFragments(operation, true).ShouldBeNull();
            context.GetRecursivelyReferencedFragments([operation]).ShouldBeNull();
            context.GetRecursivelyReferencedFragments([operation, operation]).ShouldBeNull();
            context.GetRecursivelyReferencedFragments(operations: []).ShouldBeNull();
        }
    }

    [Fact]
    public void ResetClearsFragmentSets()
    {
        var operation = new GraphQLOperationDefinition(new GraphQLSelectionSet([Spread("fragment")]));
        var original = Fragment("fragment", new GraphQLSelectionSet([new GraphQLField(new GraphQLName("oldValue"))]));
        var context = Context(new GraphQLDocument([operation, original]), operation);
        context.GetRecursivelyReferencedFragments(operation).ShouldBe([original]);
        context.GetRecursivelyReferencedFragments(operation, true).ShouldBe([original]);

        context.Reset();
        var replacement = Fragment("fragment", new GraphQLSelectionSet([new GraphQLField(new GraphQLName("newValue"))]));
        context.Document = new GraphQLDocument([operation, replacement]);
        context.Operation = operation;
        context.Variables = Inputs.Empty;

        context.GetRecursivelyReferencedFragments(operation).ShouldBe([replacement]);
        context.GetRecursivelyReferencedFragments(operation, true).ShouldBe([replacement]);
    }

    private static ValidationContext Context(GraphQLDocument document, GraphQLOperationDefinition operation)
        => new() { Document = document, Operation = operation, Variables = Inputs.Empty };

    private static GraphQLFragmentSpread Spread(string name)
        => new(new GraphQLFragmentName(new GraphQLName(name)));

    private static GraphQLFragmentDefinition Fragment(string name, GraphQLSelectionSet selectionSet)
        => new(new GraphQLFragmentName(new GraphQLName(name)), new GraphQLTypeCondition(new GraphQLNamedType(new GraphQLName("Query"))), selectionSet);

    private sealed class TrackingValidationContext : ValidationContext
    {
        public List<ASTNode> Visited { get; } = [];

        public ASTNode? ThrowOn { get; set; }

        public override bool ShouldIncludeNode(ASTNode node)
        {
            Visited.Add(node);
            if (node == ThrowOn)
                throw new InvalidOperationException();
            return base.ShouldIncludeNode(node);
        }
    }
}
