using System.Linq;
using GraphQL.Language;
using GraphQL.Language.AST;
using GraphQLParser;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Language
{
    public class CommentTests
    {
        [Fact]
        public void operation_comment_should_be_null()
        {
            const string query = @"
query _ {
    person {
        name
    }
}";

            var document = CoreToVanillaConverter.Convert(Parser.Parse(query));
            document.Operations.First().Comment.ShouldBeNull();
        }

        [Fact]
        public void operation_comment_should_not_be_null()
        {
            const string query = @"#comment
query _ {
    person {
        name
    }
}";

            var document = CoreToVanillaConverter.Convert(Parser.Parse(query, new ParserOptions { Ignore = IgnoreOptions.None }));
            document.Operations.First().Comment.ShouldBe("comment");
        }

        [Fact]
        public void field_comment_should_be_null()
        {
            const string query = @"
query _ {
    person {
        name
    }
}";

            var document = CoreToVanillaConverter.Convert(Parser.Parse(query));
            document.Operations.First()
                .SelectionSet.Selections.OfType<Field>().First().Comment.ShouldBeNull();
            document.Operations.First()
                .SelectionSet.Selections.OfType<Field>().First()
                .SelectionSet.Selections.OfType<Field>().First().Comment.ShouldBeNull();
        }

        [Fact]
        public void field_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    #comment1
    person {
        #comment2
        name
    }
}";

            var document = CoreToVanillaConverter.Convert(Parser.Parse(query, new ParserOptions { Ignore = IgnoreOptions.None }));
            document.Operations.First()
                .SelectionSet.Selections.OfType<Field>().First().Comment.ShouldBe("comment1");
            document.Operations.First()
                .SelectionSet.Selections.OfType<Field>().First()
                .SelectionSet.Selections.OfType<Field>().First().Comment.ShouldBe("comment2");
        }

        [Fact]
        public void fragmentdefinition_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    person {
        ...human
    }
}

#comment
fragment human on person {
        name
}";

            var document = CoreToVanillaConverter.Convert(Parser.Parse(query, new ParserOptions { Ignore = IgnoreOptions.None }));
            document.Fragments.First().Comment.ShouldBe("comment");
        }

        [Fact]
        public void fragmentspread_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    person {
        #comment
        ...human
    }
}

fragment human on person {
        name
}";

            var document = CoreToVanillaConverter.Convert(Parser.Parse(query, new ParserOptions { Ignore = IgnoreOptions.None }));
            document.Operations.First()
                .SelectionSet.Selections.OfType<Field>().First()
                .SelectionSet.Selections.OfType<FragmentSpread>().First().Comment.ShouldBe("comment");
        }

        [Fact]
        public void inlinefragment_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    person {
        #comment
        ... on human {
            name
        }
    }
}";

            var document = CoreToVanillaConverter.Convert(Parser.Parse(query, new ParserOptions { Ignore = IgnoreOptions.None }));
            document.Operations.First()
                .SelectionSet.Selections.OfType<Field>().First()
                .SelectionSet.Selections.OfType<InlineFragment>().First().Comment.ShouldBe("comment");
        }

        [Fact]
        public void argument_comment_should_not_be_null()
        {
            const string query = @"
query _ {
    person(
        #comment
        _where: ""foo"") {
        name
    }
}";

            var document = CoreToVanillaConverter.Convert(Parser.Parse(query, new ParserOptions { Ignore = IgnoreOptions.None }));
            document.Operations.First()
                .SelectionSet.Selections.OfType<Field>().First()
                .Arguments.First().Comment.ShouldBe("comment");
        }

        [Fact]
        public void variable_comment_should_not_be_null()
        {
            const string query = @"
query _(
    #comment
    $id: ID) {
    person {
        name
    }
}";

            var document = CoreToVanillaConverter.Convert(Parser.Parse(query, new ParserOptions { Ignore = IgnoreOptions.None }));
            document.Operations.First()
                .Variables.First().Comment.ShouldBe("comment");
        }
    }
}
