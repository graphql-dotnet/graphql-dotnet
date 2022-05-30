using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL.Tests.Execution;

public class RepeatedSubfieldsTests
{
    public RepeatedSubfieldsTests()
    {
        FirstInnerField = new GraphQLField { Name = new GraphQLName("first") };
        FirstFieldSelection = new GraphQLSelectionSet
        {
            Selections = new List<ASTNode>
            {
                FirstInnerField
            }
        };
        SecondInnerField = new GraphQLField { Name = new GraphQLName("second") };
        SecondFieldSelection = new GraphQLSelectionSet
        {
            Selections = new List<ASTNode>
            {
                SecondInnerField
            }
        };
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

    private Dictionary<string, (GraphQLField Field, FieldType FieldType)> CollectFrom(ExecutionContext executionContext, IGraphType graphType, GraphQLSelectionSet selectionSet)
    {
        return new MyExecutionStrategy().MyCollectFrom(executionContext, graphType, selectionSet);
    }

    private class MyExecutionStrategy : ParallelExecutionStrategy
    {
        public Dictionary<string, (GraphQLField Field, FieldType FieldType)> MyCollectFrom(ExecutionContext executionContext, IGraphType graphType, GraphQLSelectionSet selectionSet)
            => CollectFieldsFrom(executionContext, graphType, selectionSet, null);
    }

    [Fact]
    public void BeMergedCorrectlyInCaseOfFields()
    {
        var outerSelection = new GraphQLSelectionSet
        {
            Selections = new List<ASTNode>
            {
                FirstTestField,
                SecondTestField
            }
        };

        var query = new ObjectGraphType { Name = "Query" };
        query.Fields.Add(new FieldType
        {
            Name = "test",
            ResolvedType = new StringGraphType()
        });

        var context = new ExecutionContext
        {
            Schema = new Schema { Query = query }
        };
        var fields = CollectFrom(context, query, outerSelection);

        fields.ContainsKey("test").ShouldBeTrue();
        fields["test"].Field.SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
        fields["test"].Field.SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
    }

    [Fact]
    public void NotMergeAliasedFields()
    {
        var outerSelection = new GraphQLSelectionSet
        {
            Selections = new List<ASTNode>
            {
                FirstTestField,
                AliasedTestField
            }
        };

        var query = new ObjectGraphType { Name = "Query" };
        query.Fields.Add(new FieldType
        {
            Name = "test",
            ResolvedType = new StringGraphType()
        });

        var context = new ExecutionContext
        {
            Schema = new Schema { Query = query }
        };

        var fields = CollectFrom(context, query, outerSelection);

        fields["test"].Field.SelectionSet.Selections.ShouldHaveSingleItem();
        fields["test"].Field.SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
        fields["alias"].Field.SelectionSet.Selections.ShouldHaveSingleItem();
        fields["alias"].Field.SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
    }

    [Fact]
    public void MergeFieldAndFragment()
    {
        var fragmentSelection = new GraphQLSelectionSet
        {
            Selections = new List<ASTNode>
            {
                FirstTestField
            }
        };
        var fragment = new GraphQLFragmentDefinition
        {
            FragmentName = new GraphQLFragmentName { Name = new GraphQLName("fragment") },
            TypeCondition = new GraphQLTypeCondition
            {
                Type = new GraphQLNamedType
                {
                    Name = new GraphQLName("Query")
                }
            },
            SelectionSet = fragmentSelection
        };

        var document = new GraphQLDocument
        {
            Definitions = new List<ASTNode>
            {
                fragment
            }
        };

        var query = new ObjectGraphType { Name = "Query" };
        query.Fields.Add(new FieldType
        {
            Name = "test",
            ResolvedType = new StringGraphType()
        });
        var schema = new Schema { Query = query };

        var context = new ExecutionContext
        {
            Document = document,
            Schema = schema
        };

        var fragSpread = new GraphQLFragmentSpread { FragmentName = new GraphQLFragmentName { Name = new GraphQLName("fragment") } };
        var outerSelection = new GraphQLSelectionSet
        {
            Selections = new List<ASTNode>
            {
                fragSpread,
                SecondTestField
            }
        };

        var fields = CollectFrom(context, query, outerSelection);

        fields.ShouldHaveSingleItem();
        fields["test"].Field.SelectionSet.Selections.ShouldContain(x => x == FirstInnerField);
        fields["test"].Field.SelectionSet.Selections.ShouldContain(x => x == SecondInnerField);
    }
}
