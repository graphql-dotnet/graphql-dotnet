using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class RepeatedSubfieldsShould
    {
        public RepeatedSubfieldsShould()
        {
            FirstInnerField = new Field(null, new NameNode("first"));
            FirstFieldSelection = new SelectionSet();
            FirstFieldSelection.Add(FirstInnerField);
            SecondInnerField = new Field(null, new NameNode("second"));
            SecondFieldSelection = new SelectionSet();
            SecondFieldSelection.Add(SecondInnerField);
            FirstTestField = new Field(null, new NameNode("test"));
            SecondTestField = new Field(null, new NameNode("test"));
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

        [Fact]
        public void BeMergedCorrectlyInCaseOfFields()
        {
            var outerSelection = new SelectionSet();
            outerSelection.Add(FirstTestField);
            outerSelection.Add(SecondTestField);

            var fields = ExecutionHelper.CollectFields(new ExecutionContext(), null, outerSelection);

            Assert.True(fields.ContainsKey("test"));
            Assert.Contains(fields["test"].SelectionSet.Selections, x => x.IsEqualTo(FirstInnerField));
            Assert.Contains(fields["test"].SelectionSet.Selections, x => x.IsEqualTo(SecondInnerField));
        }

        [Fact]
        public void NotMergeAliasedFields()
        {
            var outerSelection = new SelectionSet();
            outerSelection.Add(FirstTestField);
            outerSelection.Add(AliasedTestField);

            var fields = ExecutionHelper.CollectFields(new ExecutionContext(), null, outerSelection);

            Assert.Single(fields["test"].SelectionSet.Selections);
            Assert.Contains(fields["test"].SelectionSet.Selections, x => x.IsEqualTo(FirstInnerField));
            Assert.Single(fields["alias"].SelectionSet.Selections);
            Assert.Contains(fields["alias"].SelectionSet.Selections, x => x.IsEqualTo(SecondInnerField));
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

            var fragments = new Fragments();
            fragments.Add(fragment);
            
            var schema = new Schema();
            schema.RegisterType(new PersonType());

            var context = new ExecutionContext()
            {
                Fragments = fragments,
                Schema = schema
            };

            var fragSpread = new FragmentSpread(new NameNode("fragment"));
            var outerSelection = new SelectionSet();
            outerSelection.Add(fragSpread);
            outerSelection.Add(SecondTestField);

            var fields = ExecutionHelper.CollectFields(
                context,
                new PersonType(),
                outerSelection);

            Assert.Single(fields);
            Assert.Contains(fields["test"].SelectionSet.Selections,
                x => x.IsEqualTo(FirstInnerField));
            Assert.Contains(fields["test"].SelectionSet.Selections,
                x => x.IsEqualTo(SecondInnerField));
        }
    }
}