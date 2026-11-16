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

        context.GetRecursivelyReferencedFragments(operation, onlyUsed: true).ShouldBe([leaf, sibling]);
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

        context.GetRecursivelyReferencedFragments(operation).ShouldBe(expected);
    }

    [Fact]
    public void PreservesDepthFirstOrderAndIgnoresRepeatedCyclicAndUnknownFragments()
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

        context.GetRecursivelyReferencedFragments(operation)!
            .Select(x => x.FragmentName.Name.StringValue)
            .ShouldBe(["first", "shared", "tail", "second", "last"]);
    }

    [Fact]
    public void HandlesDeeplyWrappedDirectiveVariableType()
    {
        var document = Parser.Parse("""
            query($skip: Boolean = true) { ...fragment @skip(if: $skip) }
            fragment fragment on Query { value }
            """);
        var operation = document.Operation();
        var variable = operation.Variables!.Items[0];
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

        context.GetRecursivelyReferencedFragments(operation, true).ShouldBe([first, second]);

        // Selection sets belonging to fragment definitions historically bypass this hook.
        context.Visited.ShouldBe([
            operation.SelectionSet,
            outer,
            outer.SelectionSet!,
            outer.SelectionSet!.Selections[0],
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

        context.GetRecursivelyReferencedFragments(operation, true)!
            .Select(x => x.FragmentName.Name.StringValue).ShouldBe(["first", "nested", "second"]);
    }

    [Fact]
    public void CachesSeparateSetsAndListsForAllAndUsedFragments()
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

        var all = context.GetRecursivelyReferencedFragmentsSet(operation)!;
        var used = context.GetRecursivelyReferencedFragmentsSet(operation, true)!;
        all.SetEquals([included, skipped]).ShouldBeTrue();
        used.SetEquals([included]).ShouldBeTrue();
        context.GetRecursivelyReferencedFragmentsSet(operation).ShouldBeSameAs(all);
        context.GetRecursivelyReferencedFragmentsSet(operation, true).ShouldBeSameAs(used);

        var allList = context.GetRecursivelyReferencedFragments(operation)!;
        var usedList = context.GetRecursivelyReferencedFragments(operation, true)!;
        allList.ShouldBe([included, skipped]);
        usedList.ShouldBe([included]);
        context.GetRecursivelyReferencedFragments(operation).ShouldBeSameAs(allList);
        context.GetRecursivelyReferencedFragments(operation, true).ShouldBeSameAs(usedList);

        allList.Clear();
        usedList.Clear();
        all.SetEquals([included, skipped]).ShouldBeTrue();
        used.SetEquals([included]).ShouldBeTrue();
    }

    [Fact]
    public void UnionsOperationSetsWithoutChangingCachedSets()
    {
        var document = Parser.Parse("""
            query First { ...shared ...first }
            query Second { ...second ...shared }
            fragment shared on Query { value }
            fragment first on Query { value }
            fragment second on Query { value }
            """);
        var firstOperation = (GraphQLOperationDefinition)document.Definitions[0];
        var secondOperation = (GraphQLOperationDefinition)document.Definitions[1];
        var shared = (GraphQLFragmentDefinition)document.Definitions[2];
        var first = (GraphQLFragmentDefinition)document.Definitions[3];
        var second = (GraphQLFragmentDefinition)document.Definitions[4];
        var context = Context(document, firstOperation);
        var firstSet = context.GetRecursivelyReferencedFragmentsSet(firstOperation)!;
        var secondSet = context.GetRecursivelyReferencedFragmentsSet(secondOperation)!;

        context.GetRecursivelyReferencedFragmentsSet([firstOperation, secondOperation])!
            .SetEquals([shared, first, second]).ShouldBeTrue();

        firstSet.SetEquals([shared, first]).ShouldBeTrue();
        secondSet.SetEquals([second, shared]).ShouldBeTrue();
        context.GetRecursivelyReferencedFragmentsSet(firstOperation).ShouldBeSameAs(firstSet);
        context.GetRecursivelyReferencedFragmentsSet(secondOperation).ShouldBeSameAs(secondSet);
        context.GetRecursivelyReferencedFragmentsSet([firstOperation]).ShouldBeSameAs(firstSet);
    }

    [Fact]
    public void PreservesPublicOperationListOrderAndDuplicates()
    {
        var document = Parser.Parse("""
            query First { ...shared ...first }
            query Second { ...second ...shared }
            fragment shared on Query { value }
            fragment first on Query { value }
            fragment second on Query { value }
            """);
        var firstOperation = (GraphQLOperationDefinition)document.Definitions[0];
        var secondOperation = (GraphQLOperationDefinition)document.Definitions[1];
        var shared = (GraphQLFragmentDefinition)document.Definitions[2];
        var first = (GraphQLFragmentDefinition)document.Definitions[3];
        var second = (GraphQLFragmentDefinition)document.Definitions[4];
        var context = Context(document, firstOperation);
        var operations = new List<GraphQLOperationDefinition> { firstOperation, secondOperation };

        var fragments = context.GetRecursivelyReferencedFragments(operations)!;
        fragments.ShouldBe([shared, first, second, shared]);
        context.GetRecursivelyReferencedFragments(operations).ShouldNotBeSameAs(fragments);
        context.GetRecursivelyReferencedFragments([firstOperation])
            .ShouldBeSameAs(context.GetRecursivelyReferencedFragments(firstOperation));

        operations.Reverse();
        var reversed = context.GetRecursivelyReferencedFragments(operations)!;
        reversed.ShouldBe([second, shared, shared, first]);

        operations[0] = firstOperation;
        context.GetRecursivelyReferencedFragments(operations).ShouldBe([shared, first, shared, first]);
        operations.Add(secondOperation);
        context.GetRecursivelyReferencedFragments(operations).ShouldBe([shared, first, shared, first, second, shared]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetClearsFragmentSetsAndPublicLists(bool initiallyHasFragments)
    {
        var firstOperation = new GraphQLOperationDefinition(new GraphQLSelectionSet([Spread("fragment")]));
        var secondOperation = new GraphQLOperationDefinition(new GraphQLSelectionSet([Spread("fragment")]));
        var operations = new List<GraphQLOperationDefinition> { firstOperation, secondOperation };
        var oldFragment = Fragment("fragment", new GraphQLSelectionSet([new GraphQLField(new GraphQLName("oldValue"))]));
        var definitions = new List<ASTNode> { firstOperation, secondOperation };
        if (initiallyHasFragments)
            definitions.Add(oldFragment);
        var context = Context(new GraphQLDocument(definitions), firstOperation);
        var oldSet = context.GetRecursivelyReferencedFragmentsSet(firstOperation);
        var oldUsedSet = context.GetRecursivelyReferencedFragmentsSet(firstOperation, true);
        var oldList = context.GetRecursivelyReferencedFragments(firstOperation);
        var oldUsedList = context.GetRecursivelyReferencedFragments(firstOperation, true);
        if (initiallyHasFragments)
        {
            oldList.ShouldBe([oldFragment]);
        }
        else
        {
            oldList.ShouldBeNull();
        }

        context.Reset();
        var fragment = Fragment("fragment", new GraphQLSelectionSet([new GraphQLField(new GraphQLName("newValue"))]));
        context.Document = new GraphQLDocument([firstOperation, secondOperation, fragment]);
        context.Operation = firstOperation;
        context.Variables = Inputs.Empty;

        var all = context.GetRecursivelyReferencedFragmentsSet(firstOperation)!;
        var used = context.GetRecursivelyReferencedFragmentsSet(firstOperation, true)!;
        all.SetEquals([fragment]).ShouldBeTrue();
        used.SetEquals([fragment]).ShouldBeTrue();
        all.ShouldNotBeSameAs(oldSet);
        used.ShouldNotBeSameAs(oldUsedSet);
        var allList = context.GetRecursivelyReferencedFragments(firstOperation)!;
        var usedList = context.GetRecursivelyReferencedFragments(firstOperation, true)!;
        allList.ShouldBe([fragment]);
        usedList.ShouldBe([fragment]);
        allList.ShouldNotBeSameAs(oldList);
        usedList.ShouldNotBeSameAs(oldUsedList);
        context.GetRecursivelyReferencedFragments(operations).ShouldBe([fragment, fragment]);
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
