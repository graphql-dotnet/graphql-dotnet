using System.Collections.Generic;
using System.Linq;
using GraphQL.Execution;
using GraphQL.Types;
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
            string pathSegment = Assert.Single(path) as string;
            Assert.Equal("name", pathSegment);
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
            string pathSegment = Assert.Single(path) as string;
            Assert.Equal("name", pathSegment);
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
            string pathSegment = Assert.Single(path) as string;
            Assert.Equal("alias", pathSegment);
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
            string pathSegment = Assert.Single(path) as string;
            Assert.Equal("name", pathSegment);
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
