using System.Linq;
using GraphQL.Execution;
using GraphQL.Types;
using Shouldly;
using Xunit;

using AST = GraphQL.Language.AST;

namespace GraphQL.Tests.Execution
{
    public class ExecutionNodeTests
    {
        [Fact]
        public void Path_Alias()
        {
            var objectGraphType = new AliasedFieldTestObject();

            var node = new ValueExecutionNode(
                new RootExecutionNode(objectGraphType),
                new StringGraphType(),
                new AST.Field(new AST.NameNode("alias"), new AST.NameNode("name")),
                objectGraphType.GetField("value"),
                indexInParentNode: null);

            var path = node.Path.ToList();
            path.ShouldHaveSingleItem().ShouldBeOfType<string>().ShouldBe("name");
        }

        [Fact]
        public void Path_Name()
        {
            var objectGraphType = new AliasedFieldTestObject();

            var node = new ValueExecutionNode(
                new RootExecutionNode(objectGraphType),
                new StringGraphType(),
                new AST.Field(null, new AST.NameNode("name")),
                objectGraphType.GetField("value"),
                indexInParentNode: null);

            var path = node.Path.ToList();
            path.ShouldHaveSingleItem().ShouldBeOfType<string>().ShouldBe("name");
        }

        [Fact]
        public void ResponsePath_Alias()
        {
            var objectGraphType = new AliasedFieldTestObject();

            var node = new ValueExecutionNode(
                new RootExecutionNode(objectGraphType),
                new StringGraphType(),
                new AST.Field(new AST.NameNode("alias"), new AST.NameNode("name")),
                objectGraphType.GetField("value"),
                indexInParentNode: null);

            var path = node.ResponsePath.ToList();
            path.ShouldHaveSingleItem().ShouldBeOfType<string>().ShouldBe("alias");
        }

        [Fact]
        public void ResponsePath_Name()
        {
            var objectGraphType = new AliasedFieldTestObject();

            var node = new ValueExecutionNode(
                new RootExecutionNode(objectGraphType),
                new StringGraphType(),
                new AST.Field(null, new AST.NameNode("name")),
                objectGraphType.GetField("value"),
                indexInParentNode: null);

            var path = node.ResponsePath.ToList();
            path.ShouldHaveSingleItem().ShouldBeOfType<string>().ShouldBe("name");
        }
    }

    public class AliasedFieldTestObject : ObjectGraphType
    {
        public AliasedFieldTestObject()
        {
            Field<StringGraphType>(
                "value",
                resolve: context => context.FieldAst.Alias ?? context.FieldAst.Name);
        }
    }
}
