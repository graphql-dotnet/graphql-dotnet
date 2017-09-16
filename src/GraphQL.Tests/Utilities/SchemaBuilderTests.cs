using System.Linq;
using GraphQL.Language.AST;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Utilities;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class SchemaBuilderTests
    {
        [Fact]
        public void should_set_query_by_name()
        {
            var definitions = @"
                type Query {
                    id: String
                }
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var query = schema.Query;
            query.ShouldNotBeNull();
            query.Name.ShouldBe("Query");
            query.Fields.Count().ShouldBe(1);

            query.Fields.Single().Name.ShouldBe("id");
        }

        [Fact]
        public void should_set_mutation_by_name()
        {
            var definitions = @"
                type Mutation {
                    mutate: String
                }
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var mutation = schema.Mutation;
            mutation.ShouldNotBeNull();
            mutation.Name.ShouldBe("Mutation");
            mutation.Fields.Count().ShouldBe(1);

            mutation.Fields.Single().Name.ShouldBe("mutate");
        }

        [Fact]
        public void should_set_subscription_by_name()
        {
            var definitions = @"
                type Subscription {
                    subscribe: String
                }
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var subscription = schema.Subscription;
            subscription.ShouldNotBeNull();
            subscription.Name.ShouldBe("Subscription");
            subscription.Fields.Count().ShouldBe(1);

            subscription.Fields.Single().Name.ShouldBe("subscribe");
        }

        [Fact]
        public void configures_schema_from_schema_type()
        {
            var definitions = @"
                type MyQuery {
                    id: String
                }

                type MyMutation {
                    mutate: String
                }

                type MySubscription {
                    subscribe: String
                }

                schema {
                  query: MyQuery
                  mutation: MyMutation
                  subscription: MySubscription
                }
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var query = schema.Query;
            query.ShouldNotBeNull();
            query.Name.ShouldBe("MyQuery");

            var mutation = schema.Mutation;
            mutation.ShouldNotBeNull();
            mutation.Name.ShouldBe("MyMutation");

            var subscription = schema.Subscription;
            subscription.ShouldNotBeNull();
            subscription.Name.ShouldBe("MySubscription");
        }

        [Fact]
        public void builds_type_with_arguments()
        {
            var definitions = @"
                type Query {
                  post(id: ID = 1): String
                }
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var query = schema.Query;
            query.Fields.Count().ShouldBe(1);

            var field = query.Fields.Single();
            field.Name.ShouldBe("post");
            field.Arguments.Count.ShouldBe(1);
            field.ResolvedType.Name.ShouldBe("String");

            var arg = field.Arguments.Single();
            arg.Name.ShouldBe("id");
            arg.DefaultValue.ShouldBe(1);
            arg.ResolvedType.Name.ShouldBe("ID");
        }

        [Fact]
        public void builds_interface()
        {
            var definitions = @"
                interface Pet {
                    id: ID
                }
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var type = schema.FindType("Pet") as InterfaceGraphType;
            type.ShouldNotBeNull();
            type.Fields.Count().ShouldBe(1);

            var field = type.Fields.Single();
            field.Name.ShouldBe("id");
            field.ResolvedType.Name.ShouldBe("ID");
        }

        [Fact]
        public void builds_enum()
        {
            var definitions = @"
                enum PetKind {
                    CAT
                    DOG
                }
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var type = schema.FindType("PetKind") as EnumerationGraphType;
            type.ShouldNotBeNull();

            type.Values.Select(x => x.Value.ToString()).ShouldBe(new [] {"CAT", "DOG"});
        }

        [Fact]
        public void builds_scalars()
        {
            var definitions = @"
                scalar CustomScalar

                type Query {
                    search: CustomScalar
                }
            ";

            var customScalar = new CustomScalarType();

            var schema = Schema.For(definitions, _ =>
            {
                _.RegisterType(customScalar);
            });

            schema.Initialize();

            var type = schema.FindType("CustomScalar") as ScalarGraphType;
            type.ShouldNotBeNull();

            var query = schema.Query;
            query.ShouldNotBeNull();

            var field = query.Fields.First();
            field.ResolvedType.ShouldBeOfType<CustomScalarType>();
        }

        [Fact]
        public void references_other_types()
        {
            var definitions = @"
                type Post {
                  id: ID!
                  title: String
                  votes: Int
                }

                type Query {
                  posts: [Post]
                  post(id: ID = 1): Post
                }
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var query = schema.Query;

            query.ShouldNotBeNull();
            query.Name.ShouldBe("Query");
            query.Fields.Count().ShouldBe(2);

            var posts = query.Fields.First();
            posts.Name.ShouldBe("posts");
            SchemaPrinter.ResolveName(posts.ResolvedType).ShouldBe("[Post]");
            query.Fields.Last().ResolvedType.Name.ShouldBe("Post");

            var post = schema.FindType("Post") as IObjectGraphType;
            post.ShouldNotBeNull();
            post.Fields.Count().ShouldBe(3);
        }

        [Fact]
        public void builds_unions()
        {
            var definitions = @"
                type Human {
                    name: String
                }

                type Droid {
                    name: String
                }

                union SearchResult = Human | Droid";

            var schema = Schema.For(definitions, _ =>
            {
                _.Types.For("Human").IsTypeOf<Human>();
                _.Types.For("Droid").IsTypeOf<Droid>();
            });

            schema.Initialize();

            var searchResult = schema.FindType("SearchResult") as UnionGraphType;
            searchResult.PossibleTypes.Select(x => x.Name).ShouldBe(new[] {"Human", "Droid"});
        }

        [Fact]
        public void builds_input_types()
        {
            var definitions = @"
                input ReviewInput {
                  stars: Int!
                  commentary: String
                }
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var input = schema.FindType("ReviewInput") as InputObjectGraphType;
            input.ShouldNotBeNull();
            input.Fields.Count().ShouldBe(2);
        }

        [Fact]
        public void builds_directives()
        {
            var definitions = @"
                directive @myDirective(
                  if: Boolean!
                ) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
            ";

            var schema = Schema.For(definitions);
            schema.Initialize();

            var directive = schema.FindDirective("myDirective");
            directive.ShouldNotBeNull();

            directive.Arguments.Count().ShouldBe(1);
            var argument = directive.Arguments.Find("if");
            SchemaPrinter.ResolveName(argument.ResolvedType).ShouldBe("Boolean!");

            directive.Locations.ShouldBe(new[]
            {
                DirectiveLocation.Field,
                DirectiveLocation.FragmentSpread,
                DirectiveLocation.InlineFragment
            });
        }

        class CustomScalarType : ScalarGraphType
        {
            public CustomScalarType()
            {
                Name = "CustomScalar";
            }

            public override object Serialize(object value)
            {
                throw new System.NotImplementedException();
            }

            public override object ParseValue(object value)
            {
                throw new System.NotImplementedException();
            }

            public override object ParseLiteral(IValue value)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
