using System.Linq;
using GraphQL.Types;
using GraphQL.Utilities;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class CustomGraphQLAttributeTests : SchemaBuilderTestBase
    {
        [Fact]
        public void can_set_metadata_from_custom_attribute()
        {
            var defs = @"
                type Post {
                    id: ID!
                    title: String!
                }

                type Query {
                    post(id: ID!): Post
                }
            ";

            Builder.Types.Include<PostWithExtraAttributesType>();

            var schema = Builder.Build(defs);
            schema.Initialize();

            var query = schema.FindType("Query") as IObjectGraphType;
            var field = query.Fields.Single(x => x.Name == "post");
            field.GetMetadata<string>("Authorize").ShouldBe("SomePolicy");
        }
    }

    public class MyAuthorizeAttribute : GraphQLAttribute
    {
        public string Policy { get; set; }

        public override void Modify(TypeConfig type)
        {
            type.Metadata["Authorize"] = Policy;
        }

        public override void Modify(FieldConfig field)
        {
            field.Metadata["Authorize"] = Policy;
        }
    }

    [GraphQLMetadata("Query")]
    public class PostWithExtraAttributesType
    {
        [GraphQLMetadata("post"), MyAuthorize(Policy = "SomePolicy")]
        public Post GetPostById(string id)
        {
            return PostData.Posts.FirstOrDefault(x => x.Id == id);
        }
    }
}
