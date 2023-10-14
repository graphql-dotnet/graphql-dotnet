using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Execution;

public class ExecutionNodeTests
{
    [Fact]
    public void RootExecutionNode_Should_Not_Throw_Exceptions()
    {
        var type = new ObjectGraphType();
        var root = new RootExecutionNode(type, null);

        root.Field.ShouldBeNull();
        root.FieldDefinition.ShouldBeNull();
        root.GetHashCode().ShouldNotBe(0);
        root.GetObjectGraphType(null).ShouldBe(type);
        root.GetParentType(null).ShouldBeNull();
        root.GraphType.ShouldBe(type);
        root.IndexInParentNode.ShouldBeNull();
        root.Name.ShouldBeNull();
        root.Parent.ShouldBeNull();
        root.Path.ToArray().Length.ShouldBe(0);
        root.ResolvedType.ShouldBeNull();
        root.ResponsePath.ToArray().Length.ShouldBe(0);
        root.Result.ShouldBeNull();
        root.Source.ShouldBeNull();
        root.SubFields.ShouldBeNull();
        root.ToString().ShouldNotBeNull();
        root.ToValue().ShouldBeNull();
        root.SelectionSet.ShouldBeNull();
    }

    [Fact]
    public void Path_Alias()
    {
        var objectGraphType = new AliasedFieldTestObject();

        var node = new ValueExecutionNode(
            new RootExecutionNode(objectGraphType, null),
            new StringGraphType(),
            new GraphQLField { Alias = new GraphQLAlias { Name = new GraphQLName("alias") }, Name = new GraphQLName("name") },
            objectGraphType.GetField("name"),
            indexInParentNode: null);

        var path = node.Path.ToList();
        path.ShouldHaveSingleItem().ShouldBeOfType<string>().ShouldBe("name");
    }

    [Fact]
    public void Path_Name()
    {
        var objectGraphType = new AliasedFieldTestObject();

        var node = new ValueExecutionNode(
            new RootExecutionNode(objectGraphType, null),
            new StringGraphType(),
            new GraphQLField { Name = new GraphQLName("name") },
            objectGraphType.GetField("name"),
            indexInParentNode: null);

        var path = node.Path.ToList();
        path.ShouldHaveSingleItem().ShouldBeOfType<string>().ShouldBe("name");
    }

    [Fact]
    public void ResponsePath_Alias()
    {
        var objectGraphType = new AliasedFieldTestObject();

        var node = new ValueExecutionNode(
            new RootExecutionNode(objectGraphType, null),
            new StringGraphType(),
            new GraphQLField { Alias = new GraphQLAlias { Name = new GraphQLName("alias") }, Name = new GraphQLName("name") },
            objectGraphType.GetField("name"),
            indexInParentNode: null);

        var path = node.ResponsePath.ToList();
        path.ShouldHaveSingleItem().ShouldBeOfType<string>().ShouldBe("alias");
    }

    [Fact]
    public void ResponsePath_Name()
    {
        var objectGraphType = new AliasedFieldTestObject();

        var node = new ValueExecutionNode(
            new RootExecutionNode(objectGraphType, null),
            new StringGraphType(),
            new GraphQLField { Name = new GraphQLName("name") },
            objectGraphType.GetField("name"),
            indexInParentNode: null);

        var path = node.ResponsePath.ToList();
        path.ShouldHaveSingleItem().ShouldBeOfType<string>().ShouldBe("name");
    }
}

public class AliasedFieldTestObject : ObjectGraphType
{
    public AliasedFieldTestObject()
    {
        Field<StringGraphType>("name")
            .Resolve(context => context.FieldAst.Alias?.Name.Value ?? context.FieldAst.Name.Value);
    }
}
