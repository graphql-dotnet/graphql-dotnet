using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class RepeatedSubfieldsTests
    {
        public RepeatedSubfieldsTests()
        {
            FirstInnerField = new Field(default, new NameNode("first"));
            FirstFieldSelection = new SelectionSet();
            FirstFieldSelection.Add(FirstInnerField);
            SecondInnerField = new Field(default, new NameNode("second"));
            SecondFieldSelection = new SelectionSet();
            SecondFieldSelection.Add(SecondInnerField);
            FirstTestField = new Field(default, new NameNode("test"));
            SecondTestField = new Field(default, new NameNode("test"));
            AliasedTestField = new Field(new NameNode("alias"), new NameNode("test"));

            FirstTestField.SelectionSet = FirstFieldSelection;
            SecondTestField.SelectionSet = SecondFieldSelection;
            AliasedTestField.SelectionSet = SecondFieldSelection;
        }

        private Field FirstInnerField { get; }
        private SelectionSet FirstFieldSelection { get; }
        private Field SecondInnerField { get; }
        private SelectionSet SecondFieldSelection { get; }
        private Field FirstTestField { get; }
        private Field SecondTestField { get; }
        private Field AliasedTestField { get; }

        private Fields CollectFrom(ExecutionContext executionContext, IGraphType graphType, SelectionSet selectionSet)
        {
            return new MyExecutionStrategy().MyCollectFrom(executionContext, graphType, selectionSet);
        }

        private class MyExecutionStrategy : ParallelExecutionStrategy
        {
            public Fields MyCollectFrom(ExecutionContext executionContext, IGraphType graphType, SelectionSet selectionSet)
                => CollectFieldsFrom(executionContext, graphType, selectionSet);
        }

        [Fact]
        public void BeMergedCorrectlyInCaseOfFields()
        {
            var outerSelection = new SelectionSet();
            outerSelection.Add(FirstTestField);
            outerSelection.Add(SecondTestField);

            var fields = CollectFrom(new ExecutionContext(), null, outerSelection);

            fields.ContainsKey("test").ShouldBeTrue();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
        }

        [Fact]
        public void NotMergeAliasedFields()
        {
            var outerSelection = new SelectionSet();
            outerSelection.Add(FirstTestField);
            outerSelection.Add(AliasedTestField);

            var fields = CollectFrom(new ExecutionContext(), null, outerSelection);

            fields["test"].SelectionSet.Selections.ShouldHaveSingleItem();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
            fields["alias"].SelectionSet.Selections.ShouldHaveSingleItem();
            fields["alias"].SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
        }

        [Fact]
        public void MergeFieldAndFragment()
        {
            var fragment = new FragmentDefinition(new NameNode("fragment"));
            var fragmentSelection = new SelectionSet();
            fragmentSelection.Add(FirstTestField);
            fragment.SelectionSet = fragmentSelection;
            fragment.Type = new GraphQL.Language.AST.NamedType(
                new NameNode("Person"));

            var fragments = new Fragments { fragment };

            var schema = new Schema();
            schema.RegisterType(new PersonType());

            var context = new ExecutionContext
            {
                Fragments = fragments,
                Schema = schema
            };

            var fragSpread = new FragmentSpread(new NameNode("fragment"));
            var outerSelection = new SelectionSet();
            outerSelection.Add(fragSpread);
            outerSelection.Add(SecondTestField);

            var fields = CollectFrom(context, new PersonType(), outerSelection);

            fields.ShouldHaveSingleItem();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
        }
    }
}
