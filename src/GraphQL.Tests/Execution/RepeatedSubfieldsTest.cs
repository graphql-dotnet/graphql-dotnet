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
            FirstInnerField = new Field(default, "first");
            FirstFieldSelection = new SelectionSet();
            FirstFieldSelection.Add(FirstInnerField);
            SecondInnerField = new Field(default, "second");
            SecondFieldSelection = new SelectionSet();
            SecondFieldSelection.Add(SecondInnerField);
            FirstTestField = new Field(default, "test");
            SecondTestField = new Field(default, "test");
            AliasedTestField = new Field("alias", "test");

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

            var fields = ExecutionHelper.CollectFields(new ExecutionContext(), null, outerSelection);

            fields["test"].SelectionSet.Selections.ShouldHaveSingleItem();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
            fields["alias"].SelectionSet.Selections.ShouldHaveSingleItem();
            fields["alias"].SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
        }

        [Fact]
        public void MergeFieldAndFragment()
        {
            var fragment = new FragmentDefinition("fragment");
            var fragmentSelection = new SelectionSet();
            fragmentSelection.Add(FirstTestField);
            fragment.SelectionSet = fragmentSelection;
            fragment.Type = new GraphQL.Language.AST.NamedType("Person");

            var fragments = new Fragments { fragment };

            var schema = new Schema();
            schema.RegisterType(new PersonType());

            var context = new ExecutionContext
            {
                Fragments = fragments,
                Schema = schema
            };

            var fragSpread = new FragmentSpread("fragment");
            var outerSelection = new SelectionSet();
            outerSelection.Add(fragSpread);
            outerSelection.Add(SecondTestField);

            var fields = ExecutionHelper.CollectFields(
                context,
                new PersonType(),
                outerSelection);

            fields.ShouldHaveSingleItem();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
            fields["test"].SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
        }
    }
}
