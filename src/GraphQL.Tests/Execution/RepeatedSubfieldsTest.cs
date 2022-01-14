using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class RepeatedSubfieldsTests
    {
        public RepeatedSubfieldsTests()
        {
            FirstInnerField = new GraphQLField { Name = new GraphQLName("first") };
            FirstFieldSelection = new GraphQLSelectionSet();
            FirstFieldSelection.Selections.Add(FirstInnerField);
            SecondInnerField = new GraphQLField { Name = new GraphQLName("second") };
            SecondFieldSelection = new GraphQLSelectionSet();
            SecondFieldSelection.Selections.Add(SecondInnerField);
            FirstTestField = new GraphQLField { Name = new GraphQLName("test") };
            SecondTestField = new GraphQLField { Name = new GraphQLName("test") };
            AliasedTestField = new GraphQLField { Alias = new GraphQLAlias { Name = new GraphQLName("alias") }, Name = new GraphQLName("test") };

            FirstTestField.SelectionSet = FirstFieldSelection;
            SecondTestField.SelectionSet = SecondFieldSelection;
            AliasedTestField.SelectionSet = SecondFieldSelection;
        }

        private GraphQLField FirstInnerField { get; }
        private GraphQLSelectionSet FirstFieldSelection { get; }
        private GraphQLField SecondInnerField { get; }
        private GraphQLSelectionSet SecondFieldSelection { get; }
        private GraphQLField FirstTestField { get; }
        private GraphQLField SecondTestField { get; }
        private GraphQLField AliasedTestField { get; }

        private Dictionary<string, GraphQLField> CollectFrom(ExecutionContext executionContext, IGraphType graphType, GraphQLSelectionSet selectionSet)
        {
            return new MyExecutionStrategy().MyCollectFrom(executionContext, graphType, selectionSet);
        }

        private class MyExecutionStrategy : ParallelExecutionStrategy
        {
            public Dictionary<string, GraphQLField> MyCollectFrom(ExecutionContext executionContext, IGraphType graphType, GraphQLSelectionSet selectionSet)
                => CollectFieldsFrom(executionContext, graphType, selectionSet, null);
        }

        [Fact]
        public void BeMergedCorrectlyInCaseOfFields()
        {
            var outerSelection = new GraphQLSelectionSet();
            outerSelection.Selections.Add(FirstTestField);
            outerSelection.Selections.Add(SecondTestField);

            var fields = CollectFrom(new ExecutionContext(), null, outerSelection);

            fields.ContainsKey("test").ShouldBeTrue();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
        }

        [Fact]
        public void NotMergeAliasedFields()
        {
            var outerSelection = new GraphQLSelectionSet();
            outerSelection.Selections.Add(FirstTestField);
            outerSelection.Selections.Add(AliasedTestField);

            var fields = CollectFrom(new ExecutionContext(), null, outerSelection);

            fields["test"].SelectionSet.Selections.ShouldHaveSingleItem();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
            fields["alias"].SelectionSet.Selections.ShouldHaveSingleItem();
            fields["alias"].SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
        }

        [Fact]
        public void MergeFieldAndFragment()
        {
            var fragmentSelection = new GraphQLSelectionSet();
            fragmentSelection.Selections.Add(FirstTestField);
            var fragment = new GraphQLFragmentDefinition
            {
                Name = new GraphQLName("fragment"),
                TypeCondition = new GraphQLTypeCondition
                {
                    Type = new GraphQLNamedType
                    {
                        Name = new GraphQLName("Person")
                    }
                },
                SelectionSet = fragmentSelection
            };

            var document = new GraphQLDocument();
            document.Definitions.Add(fragment);

            var schema = new Schema();
            schema.RegisterType(new PersonType());

            var context = new ExecutionContext
            {
                Document = document,
                Schema = schema
            };

            var fragSpread = new GraphQLFragmentSpread { Name = new GraphQLName("fragment") };
            var outerSelection = new GraphQLSelectionSet();
            outerSelection.Selections.Add(fragSpread);
            outerSelection.Selections.Add(SecondTestField);

            var fields = CollectFrom(context, new PersonType(), outerSelection);

            fields.ShouldHaveSingleItem();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
        }
    }
}
