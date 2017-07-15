using System;
using System.Linq;
using GraphQL.Types;
using GraphQL.Utilities;
using Shouldly;
using Xunit;

namespace GraphQL.Tools.Tests
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

            var builder = new SchemaBuilder();
            var schema = builder.Build(definitions);

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

            var builder = new SchemaBuilder();
            var schema = builder.Build(definitions);

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

            var builder = new SchemaBuilder();
            var schema = builder.Build(definitions);

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

            var builder = new SchemaBuilder();
            var schema = builder.Build(definitions);

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

            var builder = new SchemaBuilder();
            var schema = builder.Build(definitions);
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

            var builder = new SchemaBuilder();
            var schema = builder.Build(definitions);
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

            var builder = new SchemaBuilder();
            var schema = builder.Build(definitions);
            schema.Initialize();

            var type = schema.FindType("PetKind") as EnumerationGraphType;
            type.ShouldNotBeNull();

            type.Values.Select(x => x.Value.ToString()).ShouldBe(new [] {"CAT", "DOG"});
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

            var builder = new SchemaBuilder();

            var schema = builder.Build(definitions);
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
    }
}
