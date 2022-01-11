using System;
using GraphQL.Language.AST;
using GraphQLParser.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Language
{
    public class ValueNodeTests
    {
        [Fact]
        public void floatvalue_cannot_contain_nan()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new FloatValue(double.NaN));
        }

        [Fact]
        public void stringvalue_cannot_contain_null()
        {
            Should.Throw<ArgumentNullException>(() => new StringValue(null));
        }

        [Fact]
        public void namenode_cannot_contain_null()
        {
            Should.Throw<ArgumentNullException>(() => new NameNode(null));
        }

        [Fact]
        public void namenode_can_contain_empty_location()
        {
            new NameNode("a").SourceLocation.ShouldBe(default(GraphQLLocation));
            new NameNode("a", default(GraphQLLocation)).SourceLocation.ShouldBe(default(GraphQLLocation));
        }

        [Fact]
        public void enumvalue_cannot_contain_null()
        {
            Should.Throw<ArgumentNullException>(() => new EnumValue(null));
        }

        [Fact]
        public void enumvalue_cannot_contain_invalid_characters()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new EnumValue("kebab-enum"));
            Should.Throw<ArgumentOutOfRangeException>(() => new EnumValue(new NameNode("kebab-enum")));
        }
    }
}
