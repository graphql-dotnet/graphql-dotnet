using GraphQL.Compilation;
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

            var fields = QueryCompilation.CollectFields(new Schema(), new Variables(), new Document(), null, outerSelection);

            fields.ContainsKey("test").ShouldBeTrue();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x.IsEqualTo(FirstInnerField));
            fields["test"].SelectionSet.Selections.ShouldContain(x => x.IsEqualTo(SecondInnerField));
        }

        [Fact]
        public void NotMergeAliasedFields()
        {
            var outerSelection = new SelectionSet();
            outerSelection.Add(FirstTestField);
            outerSelection.Add(AliasedTestField);

            var fields = QueryCompilation.CollectFields(new Schema(), new Variables(), new Document(), null, outerSelection);

            fields["test"].SelectionSet.Selections.ShouldHaveSingleItem();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x.IsEqualTo(FirstInnerField));
            fields["alias"].SelectionSet.Selections.ShouldHaveSingleItem();
            fields["alias"].SelectionSet.Selections.ShouldContain(x => x.IsEqualTo(SecondInnerField));
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
            var document = new Document();
            document.AddDefinition(fragment);

            var schema = new Schema();
            schema.RegisterType(new PersonType());

            var fragSpread = new FragmentSpread(new NameNode("fragment"));
            var outerSelection = new SelectionSet();
            outerSelection.Add(fragSpread);
            outerSelection.Add(SecondTestField);

            var fields = QueryCompilation.CollectFields(
                schema,
                new Variables(),
                document,
                new PersonType(),
                outerSelection);

            fields.ShouldHaveSingleItem();
            fields["test"].SelectionSet.Selections.ShouldContain(x => x.IsEqualTo(FirstInnerField));
            fields["test"].SelectionSet.Selections.ShouldContain(x => x.IsEqualTo(SecondInnerField));
        }
    }
}
